// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Core.Features.Health;

/// <summary>
/// Asynchronously waits for the cached data to be set before the data can be retrieved
/// </summary>
/// <typeparam name="T">The cached data type</typeparam>
public class AsyncCache<T> where T : class
{
    private T _cachedData;
    private readonly TaskCompletionSource<bool> _initializationTask = new TaskCompletionSource<bool>();

    public async Task<T> GetCachedData()
    {
        if (_cachedData == null)
        {
            await _initializationTask.Task.ConfigureAwait(false);
        }

        return _cachedData;
    }

    public void SetCachedData(T cachedData)
    {
        EnsureArg.IsNotNull(cachedData, nameof(cachedData));

        _cachedData = cachedData;

        _initializationTask.TrySetResult(true);
    }
}
