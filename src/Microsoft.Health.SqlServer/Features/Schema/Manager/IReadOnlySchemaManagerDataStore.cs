// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    /// <summary>
    /// Represents a read-only interface for querying versioning information
    /// </summary>
    public interface IReadOnlySchemaManagerDataStore
    {
        /// <summary>
        /// Asynchronously retrieves the current application database version whose status indicates completion.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the <see cref="GetCurrentSchemaVersionAsync(CancellationToken)"/> operation.
        /// The value of its <see cref="Task{TResult}.Result"/> property contains the numeric version identifier
        /// for the latest completed version, if found; otherwise ><c>0</c>.
        /// </returns>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        Task<int> GetCurrentSchemaVersionAsync(CancellationToken cancellationToken);
    }
}
