// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core;

namespace Microsoft.Health.Api.Features.HealthChecks
{
    internal sealed class CachedHealthCheck : IHealthCheck, IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly Func<IServiceProvider, IHealthCheck> _healthCheckFactory;
        private readonly HealthCheckCachingOptions _options;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // By default, the times are DateTimeOffset.MinValue such that they are always considered invalid
        private volatile CachedHealthCheckResult _cachedResult = new CachedHealthCheckResult();

        public CachedHealthCheck(
            IServiceProvider provider,
            Func<IServiceProvider, IHealthCheck> healthCheckFactory,
            HealthCheckCachingOptions options,
            ILogger<CachedHealthCheck> logger)
        {
            _provider = EnsureArg.IsNotNull(provider, nameof(provider));
            _healthCheckFactory = EnsureArg.IsNotNull(healthCheckFactory, nameof(healthCheckFactory));
            _options = EnsureArg.IsNotNull(options, nameof(options));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            DateTimeOffset currentTime = Clock.UtcNow;
            if (IsCacheFresh(currentTime))
            {
                return Task.FromResult(_cachedResult.Value);
            }

            // If the cache is stale but not expired, then we make a best effort to refresh if the semaphore isn't
            // currently occupied by another thread. Otherwise, we'll use the cached value.
            TimeSpan maxWait = IsCacheValid(currentTime) ? TimeSpan.Zero : Timeout.InfiniteTimeSpan;
            return RefreshCache(context, maxWait, cancellationToken);
        }

        private async Task<HealthCheckResult> RefreshCache(HealthCheckContext context, TimeSpan maxSemaphoreWait, CancellationToken cancellationToken)
        {
            try
            {
                // Note: Only the timespan controls whether or not the method returns true/false.
                //       If the token is canceled, the method throws instead an OperationCanceledException.
                if (!await _semaphore.WaitAsync(maxSemaphoreWait, cancellationToken).ConfigureAwait(false))
                {
                    return _cachedResult.Value;
                }
            }
            catch (OperationCanceledException oce)
            {
                // Re-evaluate the current time and return the cached value if it's still valid
                if (!IsCacheValid(Clock.UtcNow))
                {
                    _logger.LogWarning(oce, $"Cancellation was requested for {nameof(CheckHealthAsync)}.");
                    throw;
                }

                return _cachedResult.Value;
            }

            try
            {
                // Once we've entered the semaphore, check the cache again for its freshness
                if (IsCacheFresh(Clock.UtcNow))
                {
                    return _cachedResult.Value;
                }

                using IServiceScope scope = _provider.CreateScope();

                HealthCheckResult result;
                try
                {
                    IHealthCheck check = _healthCheckFactory.Invoke(scope.ServiceProvider);
                    result = await check.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce)
                {
                    // Re-evaluate the current time and return the cached value if it's still valid
                    if (!IsCacheValid(Clock.UtcNow))
                    {
                        _logger.LogWarning(oce, $"Cancellation was requested for {nameof(CheckHealthAsync)}.");
                        throw;
                    }

                    return _cachedResult.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check failed to complete.");
                    result = HealthCheckResult.Unhealthy(Resources.FailedHealthCheckMessage); // Do not pass error to caller
                }

                return UpdateCache(result).Value;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private CachedHealthCheckResult UpdateCache(HealthCheckResult result)
        {
            // Only update the cache if:
            // (1) The current result has expired OR
            // (2) The current result is stale and the cache has been configured to save failed results OR
            // (3) The current result is stale and the new result is healthy
            DateTimeOffset currentTime = Clock.UtcNow;
            if (currentTime >= _cachedResult.ExpireTime || _options.CacheFailure || result.Status != HealthStatus.Unhealthy)
            {
                _cachedResult = new CachedHealthCheckResult
                {
                    ExpireTime = currentTime + _options.Expiry,
                    StaleTime = currentTime + _options.Expiry - _options.RefreshOffset,
                    Value = result,
                };
            }

            return _cachedResult;
        }

        private bool IsCacheFresh(DateTimeOffset currentTime)
            => currentTime < _cachedResult.StaleTime && (_options.CacheFailure || _cachedResult.Value.Status != HealthStatus.Unhealthy);

        private bool IsCacheValid(DateTimeOffset currentTime)
            => currentTime < _cachedResult.ExpireTime;

        public void Dispose()
        {
            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        private sealed class CachedHealthCheckResult
        {
            public DateTimeOffset StaleTime { get; init; }

            public DateTimeOffset ExpireTime { get; init; }

            public HealthCheckResult Value { get; init; }
        }
    }
}
