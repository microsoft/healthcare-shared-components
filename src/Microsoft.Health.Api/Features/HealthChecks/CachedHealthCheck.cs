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

internal sealed class CachedHealthCheck : IHealthCheck, IDisposable
{
    private readonly IHealthCheck _healthCheck;
    private readonly HealthCheckCachingOptions _options;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore;

    // By default, the times are DateTimeOffset.MinValue such that they are always considered invalid
    private CachedHealthCheckResult _cache = new CachedHealthCheckResult();

#if NET8_0_OR_GREATER
    private readonly TimeProvider _timeProvider;

    public CachedHealthCheck(IHealthCheck healthCheck, IOptions<HealthCheckCachingOptions> options, ILoggerFactory loggerFactory)
        : this(healthCheck, TimeProvider.System, options, loggerFactory)
    { }

    internal CachedHealthCheck(IHealthCheck healthCheck, TimeProvider timeProvider, IOptions<HealthCheckCachingOptions> options, ILoggerFactory loggerFactory)
    {
        _timeProvider = EnsureArg.IsNotNull(timeProvider, nameof(timeProvider));
#else
    public CachedHealthCheck(IHealthCheck healthCheck, IOptions<HealthCheckCachingOptions> options, ILoggerFactory loggerFactory)
    {
#endif
        _healthCheck = EnsureArg.IsNotNull(healthCheck, nameof(healthCheck));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _logger = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory)).CreateLogger(healthCheck.GetType().FullName);
        _semaphore = new SemaphoreSlim(_options.MaxRefreshThreads, _options.MaxRefreshThreads);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        DateTimeOffset currentTime = _timeProvider.GetUtcNow();
#else
        DateTimeOffset currentTime = Clock.UtcNow;
#endif
        if (!IsUpToDate(currentTime))
        {
            await RefreshCacheAsync(context, !HasExpired(currentTime), cancellationToken).ConfigureAwait(false);
        }

        return _cache.Result;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task RefreshCacheAsync(HealthCheckContext context, bool canSkip, CancellationToken cancellationToken)
    {
        // If the cache is stale but not expired, then we make a best effort to refresh if the semaphore isn't
        // currently occupied by another thread. Otherwise, we'll use the cached value.
        if (!await TryGetSemaphoreAsync(canSkip ? TimeSpan.Zero : Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false))
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
                HealthCheckResult result = await _healthCheck.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false);
                RefreshCache(result);
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
                _logger.LogWarning(oce, "Health check was canceled. Falling back to cache.");
            }
            else
            {
                _logger.LogError(ex, "Health check failed to complete. Falling back to cache.");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void RefreshCache(HealthCheckResult newResult)
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
        }
    }

    private bool HasExpired(DateTimeOffset timestamp)
        => timestamp >= _cache.ExpireTime;

    private bool IsUpToDate(DateTimeOffset timestamp)
        => timestamp < _cache.StaleTime;

    private async Task<bool> TryGetSemaphoreAsync(TimeSpan timeout, CancellationToken cancellationToken)
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
            _logger.LogWarning(oce, "Health check was canceled. Falling back to cache.");
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
