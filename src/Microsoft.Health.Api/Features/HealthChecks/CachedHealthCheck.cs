// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core;

namespace Microsoft.Health.Api.Features.HealthChecks
{
    internal sealed class CachedHealthCheck : IHealthCheck, IDisposable
    {
        private readonly IHealthCheck _healthCheck;
        private readonly HealthCheckCachingOptions _options;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore;

        // By default, the times are DateTimeOffset.MinValue such that they are always considered invalid
        private CachedHealthCheckResult _cachedResult = new CachedHealthCheckResult();

        public CachedHealthCheck(
            IHealthCheck healthCheck,
            IOptions<HealthCheckCachingOptions> options,
            ILoggerFactory loggerFactory)
        {
            _healthCheck = EnsureArg.IsNotNull(healthCheck, nameof(healthCheck));
            _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
            _logger = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory)).CreateLogger(healthCheck.GetType().FullName);
            _semaphore = new SemaphoreSlim(_options.MaxRefreshThreads, _options.MaxRefreshThreads);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            CacheSnapshot current = GetCacheSnapshot();
            return current.RequiresRefresh ? RefreshCache(context, current, cancellationToken) : Task.FromResult(current.Result);
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<HealthCheckResult> RefreshCache(
            HealthCheckContext context,
            CacheSnapshot current,
            CancellationToken cancellationToken)
        {
            // If the cache is stale but not expired, then we make a best effort to refresh if the semaphore isn't
            // currently occupied by another thread. Otherwise, we'll use the cached value.
            TimeSpan maxWait = current.HasExpired ? Timeout.InfiniteTimeSpan : TimeSpan.Zero;

            try
            {
                // Note: Only the timespan controls whether or not the method returns true/false.
                //       If the token is canceled, the method throws instead an OperationCanceledException.
                if (!await _semaphore.WaitAsync(maxWait, cancellationToken).ConfigureAwait(false))
                {
                    return _cachedResult.Result;
                }
            }
            catch (OperationCanceledException oce)
            {
                // Re-evaluate the current time and return the cached value if it's still valid
                current = GetCacheSnapshot();
                if (current.HasExpired)
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
                if (!current.RequiresRefresh)
                {
                    return current.Result;
                }

                HealthCheckResult result;
                try
                {
                    result = await _healthCheck.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce)
                {
                    // Re-evaluate the current time and return the cached value if it's still valid
                    current = GetCacheSnapshot();
                    if (current.HasExpired)
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

        private HealthCheckResult RefreshCache(CacheSnapshot expectedCurrent, HealthCheckResult newResult)
        {
            // The cache if it's not up-to-date or we found a better status
            if (expectedCurrent.RequiresRefresh || newResult.Status > expectedCurrent.Result.Status)
            {
                var newCache = new CachedHealthCheckResult
                {
                    ExpireTime = expectedCurrent.Timestamp + _options.Expiry,
                    StaleTime = expectedCurrent.Timestamp + _options.Expiry - _options.RefreshOffset,
                    Result = newResult,
                };

                // Other threads may have updated it while we were checking the cache's validity,
                // so we need to check what Interlock.CompareExchange found to be the previous cached value.
                CachedHealthCheckResult actualCurrentCache = Interlocked.CompareExchange(ref _cachedResult, newCache, expectedCurrent.Cache);
                return ReferenceEquals(actualCurrentCache, expectedCurrent.Cache) ? newResult : actualCurrentCache.Result;
            }

            // Otherwise, return whatever is the current cache
            return _cachedResult.Result;
        }

        private CacheSnapshot GetCacheSnapshot()
            => new CacheSnapshot { Cache = _cachedResult, Timestamp = Clock.UtcNow };

        private readonly struct CacheSnapshot
        {
            public DateTimeOffset Timestamp { get; init; }

            public CachedHealthCheckResult Cache { get; init; }

            public HealthCheckResult Result => Cache.Result;

            public bool RequiresRefresh => Timestamp >= Cache.StaleTime;

            public bool HasExpired => Timestamp >= Cache.ExpireTime;
        }

        private sealed class CachedHealthCheckResult
        {
            public DateTimeOffset StaleTime { get; init; }

            public DateTimeOffset ExpireTime { get; init; }

            public HealthCheckResult Result { get; init; }
        }
    }
}
