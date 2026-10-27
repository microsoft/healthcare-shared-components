// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents an <see cref="ISqlConnectionBuilder"/> that creates connections based on the user settings.
/// </summary>
public class DefaultSqlConnectionBuilder : ISqlConnectionBuilder
{
    private readonly SqlServerOptions _options;
    private readonly SqlServerDatabaseOptions _databaseOptions;
    private readonly SqlRetryLogicBaseProvider _retryProvider;

    /// <summary>
    /// Creates a new instance of the <see cref="DefaultSqlConnectionBuilder"/> class.
    /// </summary>
    /// <param name="options">The SQL data store options containing information such as the connection string.</param>
    /// <param name="retryProvider">A retry provider to ensure better connection resiliency.</param>
    /// <exception cref="ArgumentException">
    /// <para>More than one database is specified.</para>
    /// <para>-or-</para>
    /// <para>The connection string is <see langword="null"/> or white space.</para>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> or <paramref name="retryProvider"/> is <see langword="null"/>.
    /// </exception>
    public DefaultSqlConnectionBuilder(IOptions<SqlServerOptions> options, SqlRetryLogicBaseProvider retryProvider)
    {
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _retryProvider = EnsureArg.IsNotNull(retryProvider, nameof(retryProvider));
        _databaseOptions = EnsureArg.IsNotNull(_options.Databases?.Values.FirstOrDefault(), nameof(options));

        EnsureArg.Is(_options.Databases.Count, 1, nameof(options), o => o.WithMessage("Only one database may be specified."));
    }

    /// <inheritdoc />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller must dispose result.")]
    public SqlConnection CreateConnection(Action<SqlConnectionStringBuilder> configure = null)
    {
        SqlConnectionStringBuilder builder = new(_databaseOptions.ConnectionString);
        configure?.Invoke(builder);

        return new SqlConnection(builder.ToString()) { RetryLogicProvider = _retryProvider };
    }
}
