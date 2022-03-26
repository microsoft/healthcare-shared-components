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

namespace Microsoft.Health.Api.Features.HealthChecks;

internal sealed class CachedHealthCheck : IHealthCheck, IDisposable
{
    private readonly IHealthCheck _healthCheck;
    private readonly HealthCheckCachingOptions _options;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore;

    // By default, the times are DateTimeOffset.MinValue such that they are always considered invalid
    private CachedHealthCheckResult _cache = new CachedHealthCheckResult();

    public CachedHealthCheck(IHealthCheck healthCheck, IOptions<HealthCheckCachingOptions> options, ILoggerFactory loggerFactory)
    {
        _healthCheck = EnsureArg.IsNotNull(healthCheck, nameof(healthCheck));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory)).CreateLogger(healthCheck.GetType().FullName);
        _semaphore = new SemaphoreSlim(_options.MaxRefreshThreads, _options.MaxRefreshThreads);
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        DateTimeOffset currentTime = Clock.UtcNow;
        return IsUpToDate(currentTime)
            ? Task.FromResult(_cache.Result)
            : RefreshCache(context, !HasExpired(currentTime), cancellationToken);
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<HealthCheckResult> RefreshCache(HealthCheckContext context, bool canSkip, CancellationToken cancellationToken)
    {
        // If the cache is stale but not expired, then we make a best effort to refresh if the semaphore isn't
        // currently occupied by another thread. Otherwise, we'll use the cached value.
        TimeSpan maxWait = canSkip ? TimeSpan.Zero : Timeout.InfiniteTimeSpan;

        try
        {
            // Note: Only the timespan controls whether or not the method returns true/false.
            //       If the token is canceled, the method throws instead an OperationCanceledException.
            if (!await _semaphore.WaitAsync(maxWait, cancellationToken).ConfigureAwait(false))
            {
                return _cache.Result;
            }
        }
        catch (OperationCanceledException oce) when (!HasExpired(Clock.UtcNow))
        {
            _logger.LogWarning(oce, "Health check was canceled. Falling back to cache.");
            return _cache.Result;
        }

        try
        {
            // Once we've entered the semaphore, check the cache again for its freshness
            if (IsUpToDate(Clock.UtcNow))
            {
                return _cache.Result;
            }

            HealthCheckResult result;
            try
            {
                result = await _healthCheck.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!HasExpired(Clock.UtcNow))
            {
                if (ex is OperationCanceledException oce)
                {
                    _logger.LogWarning(oce, "Health check was canceled. Falling back to cache.");
                }
                else
                {
                    _logger.LogError(ex, "Health check failed to complete. Falling back to cache.");
                }

                return _cache.Result;
            }

            // Update cache based on the latest snapshot
            return RefreshCache(result);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private HealthCheckResult RefreshCache(HealthCheckResult newResult)
    {
        // The cache if it's not up-to-date or we found a better status
        DateTimeOffset currentTime = Clock.UtcNow;
        CachedHealthCheckResult currentCache = _cache;

        if (currentTime >= currentCache.StaleTime || newResult.Status > currentCache.Result.Status)
        {
            var newCache = new CachedHealthCheckResult
            {
                ExpireTime = currentTime + _options.Expiry,
                StaleTime = currentTime + _options.Expiry - _options.RefreshOffset,
                Result = newResult,
            };

            // Other threads may have updated it while we were checking the cache's validity,
            // so we need to check what Interlock.CompareExchange found to be the previous cached value.
            CachedHealthCheckResult actualCurrentCache = Interlocked.CompareExchange(ref _cache, newCache, currentCache);
            return ReferenceEquals(actualCurrentCache, currentCache) ? newResult : actualCurrentCache.Result;
        }

        // Otherwise, return whatever is the current cache
        return _cache.Result;
    }

    private bool HasExpired(DateTimeOffset timestamp)
        => timestamp >= _cache.ExpireTime;

    private bool IsUpToDate(DateTimeOffset timestamp)
        => timestamp < _cache.StaleTime;

    private sealed class CachedHealthCheckResult
    {
        public DateTimeOffset StaleTime { get; init; }

        public DateTimeOffset ExpireTime { get; init; }

        public HealthCheckResult Result { get; init; }
    }
}
