// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents a base <see cref="ISqlConnectionBuilder"/> that creates connections for a single configured database.
/// </summary>
public abstract class SqlConnectionBuilder : ISqlConnectionBuilder
{
    private readonly SqlServerDatabaseOptions _options;
    private readonly SqlRetryLogicBaseProvider _retryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlConnectionBuilder"/> class.
    /// </summary>
    /// <param name="options">The options for the database whose connection string is used to create connections.</param>
    /// <param name="retryProvider">The retry logic provider assigned to each created connection.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> or <paramref name="retryProvider"/> is <see langword="null"/>.
    /// </exception>
    protected SqlConnectionBuilder(SqlServerDatabaseOptions options, SqlRetryLogicBaseProvider retryProvider)
    {
        _options = EnsureArg.IsNotNull(options, nameof(options));
        _retryProvider = EnsureArg.IsNotNull(retryProvider, nameof(retryProvider));

        DefaultDatabase = new SqlConnectionStringBuilder(_options.ConnectionString).InitialCatalog;
    }

    /// <inheritdoc/>
    public string DefaultDatabase { get; }

    /// <inheritdoc/>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller must dispose result.")]
    public SqlConnection CreateConnection(Action<SqlConnectionStringBuilder>? configure = null)
    {
        SqlConnectionStringBuilder builder = new(_options.ConnectionString);
        configure?.Invoke(builder);

        return new SqlConnection(builder.ToString()) { RetryLogicProvider = _retryProvider };
    }
}
