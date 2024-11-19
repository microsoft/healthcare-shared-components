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
#if !NET8_0_OR_GREATER
using Microsoft.Health.Core;
#endif

namespace Microsoft.Health.Api.Features.HealthChecks;

internal sealed class HealthCheckResultCache : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly HealthCheckCachingOptions _options;
    private readonly ILoggerFactory _loggerFactory;

    // By default, the times are DateTimeOffset.MinValue such that they are always considered expired
    private CachedHealthCheckResult _cache = new CachedHealthCheckResult { Result = new HealthCheckResult(HealthStatus.Unhealthy) };

#if NET8_0_OR_GREATER
    private readonly TimeProvider _timeProvider;

    public HealthCheckResultCache(IOptions<HealthCheckCachingOptions> options, ILoggerFactory loggerFactory)
        : this(TimeProvider.System, options, loggerFactory)
    { }

    internal HealthCheckResultCache(TimeProvider timeProvider, IOptions<HealthCheckCachingOptions> options, ILoggerFactory loggerFactory)
    {
        _timeProvider = EnsureArg.IsNotNull(timeProvider, nameof(timeProvider));
#else
    public HealthCheckResultCache(IOptions<HealthCheckCachingOptions> options, ILoggerFactory loggerFactory)
    {
#endif
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _loggerFactory = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
        _semaphore = new SemaphoreSlim(_options.MaxRefreshThreads, _options.MaxRefreshThreads);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(IHealthCheck healthCheck, HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(healthCheck, nameof(healthCheck));

#if NET8_0_OR_GREATER
        DateTimeOffset currentTime = _timeProvider.GetUtcNow();
#else
        DateTimeOffset currentTime = Clock.UtcNow;
#endif
        if (!IsUpToDate(currentTime))
        {
            await RefreshCacheAsync(healthCheck, context, !HasExpired(currentTime), cancellationToken).ConfigureAwait(false);
        }

        return _cache.Result;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _loggerFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task RefreshCacheAsync(IHealthCheck healthCheck, HealthCheckContext context, bool canSkip, CancellationToken cancellationToken)
    {
        ILogger logger = _loggerFactory.CreateLogger(healthCheck.GetType().FullName);

        // If the cache is stale but not expired, then we make a best effort to refresh if the semaphore isn't
        // currently occupied by another thread. Otherwise, we'll use the cached value.
        if (!await TryGetSemaphoreAsync(canSkip ? TimeSpan.Zero : Timeout.InfiniteTimeSpan, logger, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            // Once we've entered the semaphore, check the cache again for its freshness
#if NET8_0_OR_GREATER
            if (!IsUpToDate(_timeProvider.GetUtcNow()))
#else
            if (!IsUpToDate(Clock.UtcNow))
#endif
            {
                HealthCheckResult result = await healthCheck.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                RefreshCache(result, logger);
            }
        }
#if NET8_0_OR_GREATER
        catch (Exception ex) when (!HasExpired(_timeProvider.GetUtcNow()))
#else
        catch (Exception ex) when (!HasExpired(Clock.UtcNow))
#endif
        {
            if (ex is OperationCanceledException oce)
            {
                logger.LogWarning(oce, "Health check was canceled. Falling back to cache.");
            }
            else
            {
                logger.LogError(ex, "Health check failed to complete. Falling back to cache.");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void RefreshCache(HealthCheckResult newResult, ILogger logger)
    {
        // The cache if it's not up-to-date or we found a better status
#if NET8_0_OR_GREATER
        DateTimeOffset currentTime = _timeProvider.GetUtcNow();
#else
        DateTimeOffset currentTime = Clock.UtcNow;
#endif
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
            Interlocked.CompareExchange(ref _cache, newCache, currentCache);
            logger.LogInformation("Updated cache with new health check result '{Result}'", newResult.Status);
        }
    }

    private bool HasExpired(DateTimeOffset timestamp)
        => timestamp >= _cache.ExpireTime;

    private bool IsUpToDate(DateTimeOffset timestamp)
    {
        // While we will store results whose status is < MinimumCachedHealthStatus, it will effectively
        // have no caching as IsUpToDate will return false
        CachedHealthCheckResult currentCache = _cache;
        return timestamp < currentCache.StaleTime && currentCache.Result.Status >= _options.MinimumCachedHealthStatus;
    }

    private async Task<bool> TryGetSemaphoreAsync(TimeSpan timeout, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            // Note: Only the timespan controls whether or not the method returns true/false.
            //       If the token is canceled, the method throws instead an OperationCanceledException.
            return await _semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
#if NET8_0_OR_GREATER
        catch (OperationCanceledException oce) when (!HasExpired(_timeProvider.GetUtcNow()))
#else
        catch (OperationCanceledException oce) when (!HasExpired(Clock.UtcNow))
#endif
        {
            logger.LogWarning(oce, "Health check was canceled. Falling back to cache.");
            return false;
        }
    }

    private sealed class CachedHealthCheckResult
    {
        public DateTimeOffset StaleTime { get; init; }

        public DateTimeOffset ExpireTime { get; init; }

        public HealthCheckResult Result { get; init; }
    }
}
