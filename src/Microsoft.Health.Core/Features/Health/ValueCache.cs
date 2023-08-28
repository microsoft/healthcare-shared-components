// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Core.Features.Health;

/// <summary>
/// Caches a single value of type <typeparamref name="T"/>.
/// Asynchronously waits for the data to be set before the data can be retrieved.
/// </summary>
/// <typeparam name="T">The data type</typeparam>
public class ValueCache<T> where T : class
{
    private volatile T _cachedData;
    private readonly TaskCompletionSource _init = new TaskCompletionSource();

    public async Task<T> GetCachedData(CancellationToken cancellationToken = default)
    {
        await _init.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return _cachedData;
    }

    public void SetCachedData(T cachedData)
    {
        EnsureArg.IsNotNull(cachedData, nameof(cachedData));

        _cachedData = cachedData;

        if (!_init.Task.IsCompleted)
        {
            _init.TrySetResult();
        }
    }
}
