// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    public interface ISchemaManagerDataStore
    {
        /// <summary>
        /// Execute the script and status update of the given version in SchemaVersion table under a transaction
        /// </summary>
        /// <param name="script">The script to execute</param>
        /// <param name="version">The version to update its status</param>
        /// <param name="cancellationToken">A cancellation token</param>
        Task ExecuteScriptAndCompleteSchemaVersionAsync(string script, int version, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the row for given version and status in the SchemaVersion table
        /// </summary>
        /// <param name="version">The schema version</param>
        /// <param name="status">The schema status</param>
        /// <param name="cancellationToken">A cancellation token</param>
        Task DeleteSchemaVersionAsync(int version, string status, CancellationToken cancellationToken);

        /// <summary>
        /// Retreives the current schema version information
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<int> GetCurrentSchemaVersionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Executes the given script
        /// </summary>
        /// <param name="script">The Sql script</param>
        /// <param name="cancellationToken">A cancellation token</param>
        Task ExecuteScriptAsync(string script, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if base schema already exists
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<bool> BaseSchemaExistsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Checks if there exists atleast a record in the InstanceSchema table.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        Task<bool> InstanceSchemaRecordExistsAsync(CancellationToken cancellationToken);
    }
}