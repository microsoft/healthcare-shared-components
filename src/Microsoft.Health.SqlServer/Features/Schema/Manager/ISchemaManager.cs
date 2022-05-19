// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public interface ISchemaManager
{
    /// <summary>
    /// Applies SQL schemas specified by the range in <paramref name="type"/>.
    /// </summary>
    /// <param name="connectionString">connection string to the backing SQL server.</param>
    /// <param name="server">URL to the service.</param>
    /// <param name="type">The schema version to apply.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A task.</returns>
    public Task ApplySchema(string connectionString, Uri server, MutuallyExclusiveType type, CancellationToken token = default);

    /// <summary>
    /// Gets a list of available schema versions.
    /// </summary>
    /// <param name="server">URL to the service.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of available schema versions from the service.</returns>
    public Task<IList<AvailableVersion>> GetAvailableSchema(Uri server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current schema version of the service.
    /// </summary>
    /// <param name="connectionString">connection string to the backing SQL server.</param>
    /// <param name="server">URL to the service.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of the current schema versions from the service.</returns>
    public Task<IList<CurrentVersion>> GetCurrentSchema(string connectionString, Uri server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest schema version of the db.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The latest version on the server</returns>
    public Task<int> GetLatestSchema(CancellationToken cancellationToken = default);
}
