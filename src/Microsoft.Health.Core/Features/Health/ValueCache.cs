// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

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
/// are timing out or throwing).
/// </remarks>
/// <typeparam name="T">The data type</typeparam>
public class ValueCache<T> where T : class
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _expiry;
    private volatile T _cachedData;
    private long _lastSetUtcTicks;
    private readonly TaskCompletionSource _init = new TaskCompletionSource();

    public ValueCache()
        : this(Timeout.InfiniteTimeSpan, TimeProvider.System)
    { }

    public ValueCache(TimeSpan expiry)
        : this(expiry, TimeProvider.System)
    { }

    internal ValueCache(TimeSpan expiry, TimeProvider timeProvider)
    {
        if (expiry != Timeout.InfiniteTimeSpan && expiry <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiry), expiry, "Expiry must be positive or Timeout.InfiniteTimeSpan.");
        }

        _expiry = expiry;
        _timeProvider = EnsureArg.IsNotNull(timeProvider, nameof(timeProvider));
    }

    public async Task<T> GetAsync(CancellationToken cancellationToken = default)
    {
        await _init.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        T data = _cachedData;

        if (_expiry != Timeout.InfiniteTimeSpan)
        {
            long lastSetTicks = Interlocked.Read(ref _lastSetUtcTicks);
            DateTimeOffset lastSet = new DateTimeOffset(lastSetTicks, TimeSpan.Zero);
            if (_timeProvider.GetUtcNow() - lastSet > _expiry)
            {
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
