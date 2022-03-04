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
        private readonly SemaphoreSlim _semaphore;

        // By default, the times are DateTimeOffset.MinValue such that they are always considered invalid
        private CachedHealthCheckResult _cachedResult = new CachedHealthCheckResult();

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
            _semaphore = new SemaphoreSlim(_options.MaxRefreshThreads, _options.MaxRefreshThreads);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            CacheSnapshot current = GetCacheSnapshot();
            if (current.IsFresh && (_options.CacheFailure || current.Result.Status != HealthStatus.Unhealthy))
            {
                return Task.FromResult(current.Result);
            }

            // If the cache is stale but not expired, then we make a best effort to refresh if the semaphore isn't
            // currently occupied by another thread. Otherwise, we'll use the cached value.
            TimeSpan maxWait = current.IsValid ? TimeSpan.Zero : Timeout.InfiniteTimeSpan;
            return RefreshCache(context, current, maxWait, cancellationToken);
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<HealthCheckResult> RefreshCache(
            HealthCheckContext context,
            CacheSnapshot current,
            TimeSpan maxSemaphoreWait,
            CancellationToken cancellationToken)
        {
            try
            {
                // Note: Only the timespan controls whether or not the method returns true/false.
                //       If the token is canceled, the method throws instead an OperationCanceledException.
                if (!await _semaphore.WaitAsync(maxSemaphoreWait, cancellationToken).ConfigureAwait(false))
                {
                    return _cachedResult.Result;
                }
            }
            catch (OperationCanceledException oce)
            {
                // Re-evaluate the current time and return the cached value if it's still valid
                current = GetCacheSnapshot();
                if (!current.IsValid)
                {
                    _logger.LogWarning(oce, $"Cancellation was requested for {nameof(CheckHealthAsync)}.");
                    throw;
                }

                return current.Result;
            }

            try
            {
                // Once we've entered the semaphore, check the cache again for its freshness
                current = GetCacheSnapshot();
                if (current.IsFresh && (_options.CacheFailure || current.Result.Status != HealthStatus.Unhealthy))
                {
                    return current.Result;
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
                    current = GetCacheSnapshot();
                    if (!current.IsValid)
                    {
                        _logger.LogWarning(oce, $"Cancellation was requested for {nameof(CheckHealthAsync)}.");
                        throw;
                    }

                    return current.Result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check failed to complete.");
                    result = HealthCheckResult.Unhealthy(Resources.FailedHealthCheckMessage); // Do not pass error to caller
                }

                // Update cache based on the latest snapshot
                return RefreshCache(GetCacheSnapshot(), result);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private HealthCheckResult RefreshCache(CacheSnapshot current, HealthCheckResult newResult)
        {
            // The cache is only updated if appropriate
            if (current.CanUpdate(newResult.Status, _options.CacheFailure))
            {
                var newCache = new CachedHealthCheckResult
                {
                    ExpireTime = current.Timestamp + _options.Expiry,
                    StaleTime = current.Timestamp + _options.Expiry - _options.RefreshOffset,
                    Result = newResult,
                };

                // Other threads may have updated it while we were checking the update's validity.
                // So we need to check what Interlock.CompareExchange found to be the previous cached value,
                // and return the value appropriately.
                CachedHealthCheckResult previousCache = Interlocked.CompareExchange(ref _cachedResult, newCache, current.Cache);
                return ReferenceEquals(previousCache, current.Cache) ? newResult : previousCache.Result;
            }

            return _cachedResult.Result;
        }

        private CacheSnapshot GetCacheSnapshot()
            => new CacheSnapshot { Cache = _cachedResult, Timestamp = Clock.UtcNow };

        private readonly struct CacheSnapshot
        {
            public DateTimeOffset Timestamp { get; init; }

            public CachedHealthCheckResult Cache { get; init; }

            public HealthCheckResult Result => Cache.Result;

            public bool IsFresh => Timestamp < Cache.StaleTime;

            public bool IsValid => Timestamp < Cache.ExpireTime;

            public bool IsStale => IsValid && !IsFresh;

            // Only update the cache if:
            // (1) The cache is fresh, but the new value is better
            // (2) The cache is stale and the cache has been configured to save any result
            // (3) The cache is stale and the new result is healthy
            // (4) The cache has expired
            public bool CanUpdate(HealthStatus newStatus, bool cacheFailure)
                => IsFresh
                ? newStatus > Result.Status
                : !IsValid || cacheFailure || newStatus != HealthStatus.Unhealthy;
        }

        private sealed class CachedHealthCheckResult
        {
            public DateTimeOffset StaleTime { get; init; }

            public DateTimeOffset ExpireTime { get; init; }

            public HealthCheckResult Result { get; init; }
        }
    }
}
