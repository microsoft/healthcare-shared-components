// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public interface ISchemaDataStore
    {
        /// <summary>
        /// Get compatible version.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest supported schema version from server.</returns>
        Task<CompatibleVersions> GetLatestCompatibleVersionsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get current version information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current schema versions information</returns>
        Task<List<CurrentVersionInformation>> GetCurrentVersionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Delete expired instance schema information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task</returns>
        Task DeleteExpiredInstanceSchemaAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Upsert current version information for the named instance.
        /// </summary>
        /// <param name="name">The instance name.</param>
        /// <param name="schemaInformation">The Schema information</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns current version</returns>
        Task<int> UpsertInstanceSchemaInformationAsync(string name, SchemaInformation schemaInformation, CancellationToken cancellationToken);
    }
}
