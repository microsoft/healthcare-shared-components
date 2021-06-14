// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Abstractions.Data
{
    /// <summary>
    /// Used as the source for Data of type T.
    /// </summary>
    /// <typeparam name="T">Records of Type T</typeparam>
    public interface ISource<T>
    {
        /// <summary>
        /// Fetch records from underlying source that matches type T.
        /// </summary>
        /// <param name="startId">Start Index of records.</param>
        /// <param name="pageSize">Page Size of records to fetch.</param>
        /// <param name="delayMilliseconds">Optional delay.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>IReadOnlyCollection of T.</returns>
        Task<IReadOnlyCollection<T>> FetchRecordsAsync(long startId, int pageSize, int? delayMilliseconds, CancellationToken cancellationToken);
    }
}
