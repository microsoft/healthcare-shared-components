// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents a builder of SQL connections.
/// </summary>
public interface ISqlConnectionBuilder
{
    /// <summary>
    /// Gets the name of the default database.
    /// </summary>
    /// <value>The default initial catalog if specified; otherwise <see langword="null"/>.</value>
    string DefaultDatabase { get; }

    /// <summary>
    /// Gets unopened sql connection with setting application intent to read only
    /// If initial catalog is not provided, it is determined from the connection string.
    /// </summary>
    /// <param name="initialCatalog">Initial catalog to connect to.</param>
    /// <param name="maxPoolSize">Max Sql connection pool size</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SqlConnection object.</returns>
    Task<SqlConnection> GetReadOnlySqlConnectionAsync(string initialCatalog = null, int? maxPoolSize = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unopened SqlConnection object.
    /// If initial catalog is not provided, it is determined from the connection string.
    /// </summary>
    /// <param name="initialCatalog">Initial catalog to connect to.</param>
    /// <param name="maxPoolSize">Max Sql connection pool size</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SqlConnection object.</returns>
    Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, int? maxPoolSize = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unopened SqlConnection object.
    /// </summary>
    /// <param name="isReadOnly">Flag indicating whether application has read only intent.</param>
    /// <param name="applicationName">Application name</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SqlConnection object.</returns>
    Task<SqlConnection> GetSqlConnectionAsync(bool isReadOnly, string applicationName, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches an an unopened SQL connection.
    /// </summary>
    /// <param name="initialCatalog">An optional initial catalog that may be used to override the default value.</param>
    /// <param name="maxPoolSize">An optional maximum for the SQL connection pool size.</param>
    /// <returns>An unopened SQL connection.</returns>
    SqlConnection GetSqlConnection(string initialCatalog = null, int? maxPoolSize = null);
}
