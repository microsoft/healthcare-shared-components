// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly SqlServerDataStoreConfiguration _options;
    private readonly SqlRetryLogicBaseProvider _retryProvider;

    /// <summary>
    /// Creates a new instance of the <see cref="DefaultSqlConnectionBuilder"/> class.
    /// </summary>
    /// <param name="options">The SQL data store options containing information such as the connection string.</param>
    /// <param name="retryProvider">A retry provider to ensure better connection resiliency.</param>
    /// <exception cref="ArgumentException">
    /// <para>The connection string is <see langword="null"/> or white space.</para>
    /// <para>-or-</para>
    /// <para>The managed identity client ID is missing when managed identity is specified.</para>
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> or <paramref name="retryProvider"/> is <see langword="null"/>.
    /// </exception>
    public DefaultSqlConnectionBuilder(IOptions<SqlServerDataStoreConfiguration> options, SqlRetryLogicBaseProvider retryProvider)
    {
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _retryProvider = EnsureArg.IsNotNull(retryProvider, nameof(retryProvider));

        EnsureArg.IsNotNullOrWhiteSpace(
            _options.ConnectionString,
            nameof(options),
            o => o.WithMessage("The SQL connection string cannot be null or white space."));

#pragma warning disable CS0618 // Type or member is obsolete
        if (_options.AuthenticationType == SqlServerAuthenticationType.ManagedIdentity && string.IsNullOrWhiteSpace(_options.ManagedIdentityClientId))
            throw new ArgumentException("The managed identity client ID cannot be null or white space.", nameof(options));
#pragma warning restore CS0618 // Type or member is obsolete

        var builder = new SqlConnectionStringBuilder(_options.ConnectionString);
        DefaultDatabase = builder.InitialCatalog;
    }

    /// <inheritdoc />
    public string DefaultDatabase { get; }

    /// <inheritdoc />
    public SqlConnection GetSqlConnection(string initialCatalog = null, int? maxPoolSize = null)
    {
        SqlConnectionStringBuilder builder = GetConnectionStringBuilder(initialCatalog, maxPoolSize);
        return new SqlConnection(builder.ToString()) { RetryLogicProvider = _retryProvider };
    }

    /// <inheritdoc />
    [Obsolete($"Use {nameof(GetSqlConnection)} instead.")]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller must dispose result.")]
    public Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, int? maxPoolSize = null, CancellationToken cancellationToken = default)
        => Task.FromResult(GetSqlConnection(initialCatalog, maxPoolSize));

    /// <summary>
    /// Creates a <see cref="SqlConnectionStringBuilder"/> using the configured connection string and modified based on the input.
    /// </summary>
    /// <param name="initialCatalog">An optional initial catalog that may be used to override the default value.</param>
    /// <param name="maxPoolSize">An optional maximum for the SQL connection pool size.</param>
    /// <returns>A <see cref="SqlConnectionStringBuilder"/> representing the current connection string.</returns>
    protected virtual SqlConnectionStringBuilder GetConnectionStringBuilder(string initialCatalog = null, int? maxPoolSize = null)
    {
        var builder = new SqlConnectionStringBuilder(_options.ConnectionString);

        if (initialCatalog != null)
            builder.InitialCatalog = initialCatalog;

        if (maxPoolSize.HasValue)
            builder.MaxPoolSize = maxPoolSize.Value;

#pragma warning disable CS0618 // Type or member is obsolete
        if (_options.AuthenticationType == SqlServerAuthenticationType.ManagedIdentity)
        {
            builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity;
            builder.UserID = _options.ManagedIdentityClientId;
        }
#pragma warning restore CS0618 // Type or member is obsolete

        return builder;
    }
}
