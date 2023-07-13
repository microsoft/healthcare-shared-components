// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// The default SQL connection string provider uses the connection string specified in configuration.
/// </summary>
public class DefaultSqlConnectionStringProvider : ISqlConnectionStringProvider
{
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

    public DefaultSqlConnectionStringProvider(IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration)
    {
        _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));

        if (_sqlServerDataStoreConfiguration.MaxPoolSize.HasValue)
        {
            EnsureArg.IsGt(_sqlServerDataStoreConfiguration.MaxPoolSize.Value, 0, nameof(_sqlServerDataStoreConfiguration.MaxPoolSize));
            EnsureArg.IsLt(_sqlServerDataStoreConfiguration.MaxPoolSize.Value, SqlServerDataStoreConfiguration.MaxPoolSizeLimit, nameof(_sqlServerDataStoreConfiguration.MaxPoolSize));
        }
    }

    /// <inheritdoc />
    public Task<string> GetSqlConnectionString(CancellationToken cancellationToken)
    {
        string connectionString = _sqlServerDataStoreConfiguration.ConnectionString;

        if (_sqlServerDataStoreConfiguration.MaxPoolSize.HasValue && !connectionString.Contains(SqlServerDataStoreConfiguration.MaxPoolSizeName, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult($"{connectionString};{SqlServerDataStoreConfiguration.MaxPoolSizeName}={_sqlServerDataStoreConfiguration.MaxPoolSize.Value};");
        }

        return Task.FromResult(connectionString);
    }
}
