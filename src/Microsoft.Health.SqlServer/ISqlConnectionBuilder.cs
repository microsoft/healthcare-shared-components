// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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
    /// Gets a SQL connection as additionally configured by the caller.
    /// </summary>
    /// <param name="configure">An optional delegate for configuring the connection.</param>
    /// <returns>The SQL connection.</returns>
    SqlConnection CreateConnection(Action<SqlConnectionStringBuilder> configure = null);

    /// <summary>
    /// Gets a SQL connection as additionally configured by the caller.
    /// </summary>
    /// <param name="configure">An optional delegate for configuring the connection.</param>
    /// <param name="cancellationToken">The optional cancellation token for suspending the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the create operation. The value of the
    /// <see cref="ValueTask{TResult}.Result"/> property is the connection.
    /// </returns>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    ValueTask<SqlConnection> CreateConnectionAsync(Action<SqlConnectionStringBuilder> configure = null, CancellationToken cancellationToken = default);

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
    /// <returns>SqlConnection object.</returns>
    Task<SqlConnection> GetSqlConnectionAsync(bool isReadOnly, string applicationName);

    /// <summary>
    /// Fetches an an unopened SQL connection.
    /// </summary>
    /// <param name="initialCatalog">An optional initial catalog that may be used to override the default value.</param>
    /// <param name="maxPoolSize">An optional maximum for the SQL connection pool size.</param>
    /// <returns>An unopened SQL connection.</returns>
    SqlConnection GetSqlConnection(string initialCatalog = null, int? maxPoolSize = null);
}
