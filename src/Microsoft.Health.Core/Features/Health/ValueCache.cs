// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Health.Core.Features.Health;

/// <summary>
/// Caches a single value of type <typeparamref name="T"/>.
/// Asynchronously waits for the data to be set before the data can be retrieved.
/// </summary>
/// <remarks>
/// Optionally accepts an expiry. When an expiry is configured, <see cref="GetAsync"/> returns
/// <see langword="null"/> if the most recent <see cref="Set"/> happened longer than the expiry
/// ago. This allows callers to detect when the cache has gone stale because the producer is no
/// longer publishing fresh values (for example, a health-check publisher whose underlying probes
/// are timing out or throwing). When constructed with the parameterless constructor (or with
/// <see cref="Timeout.InfiniteTimeSpan"/>), the cache never expires and <see cref="GetAsync"/>
/// only returns <see langword="null"/> in the trivial case where <typeparamref name="T"/> itself
/// is null (which is prevented by <see cref="Set"/>'s <see cref="EnsureArg"/> guard).
/// </remarks>
/// <typeparam name="T">The data type</typeparam>
public class ValueCache<T> where T : class
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _expiry;
    private readonly ILogger<ValueCache<T>> _logger;
    private volatile T? _cachedData;
    private long _lastSetUtcTicks;
    private readonly TaskCompletionSource _init = new TaskCompletionSource();

    public ValueCache()
        : this(Timeout.InfiniteTimeSpan, TimeProvider.System, NullLogger<ValueCache<T>>.Instance)
    { }

    public ValueCache(TimeSpan expiry)
        : this(expiry, TimeProvider.System, NullLogger<ValueCache<T>>.Instance)
    { }

    public ValueCache(TimeSpan expiry, ILogger<ValueCache<T>> logger)
        : this(expiry, TimeProvider.System, logger)
    { }

    internal ValueCache(TimeSpan expiry, TimeProvider timeProvider)
        : this(expiry, timeProvider, NullLogger<ValueCache<T>>.Instance)
    { }

    internal ValueCache(TimeSpan expiry, TimeProvider timeProvider, ILogger<ValueCache<T>> logger)
    {
        if (expiry != Timeout.InfiniteTimeSpan && expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), expiry, "Expiry must be positive or Timeout.InfiniteTimeSpan.");
        }

        _expiry = expiry;
        _timeProvider = EnsureArg.IsNotNull(timeProvider, nameof(timeProvider));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <summary>
    /// Returns the most recently <see cref="Set"/> value, asynchronously waiting until <see cref="Set"/>
    /// has been called at least once.
    /// </summary>
    /// <returns>
    /// The cached value, or <see langword="null"/> if an expiry was configured and the most recent
    /// <see cref="Set"/> is older than the expiry.
    /// </returns>
    public async Task<T?> GetAsync(CancellationToken cancellationToken = default)
    {
        await _init.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        T? data = _cachedData;

        if (_expiry != Timeout.InfiniteTimeSpan)
        {
            long lastSetTicks = Interlocked.Read(ref _lastSetUtcTicks);
            DateTimeOffset lastSet = new DateTimeOffset(lastSetTicks, TimeSpan.Zero);
            TimeSpan age = _timeProvider.GetUtcNow() - lastSet;
            if (age > _expiry)
            {
                _logger.LogWarning(
                    "ValueCache<{ValueType}> returning null: cached value is stale (age {AgeSeconds:F1}s exceeds expiry {ExpirySeconds:F1}s). The producer has not refreshed the cache within the expiry window.",
                    typeof(T).Name,
                    age.TotalSeconds,
                    _expiry.TotalSeconds);
                return null;
            }
        }

        return data;
    }

    public void Set(T cachedData)
    {
        EnsureArg.IsNotNull(cachedData, nameof(cachedData));

        _cachedData = cachedData;
        Interlocked.Exchange(ref _lastSetUtcTicks, _timeProvider.GetUtcNow().UtcTicks);

        if (!_init.Task.IsCompleted)
        {
            _init.TrySetResult();
        }
    }
}
