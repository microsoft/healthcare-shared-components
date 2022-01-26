// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer
{
    /// <summary>
    /// Default Sql Connection Factory is responsible to handle Sql connections that can be made purely based on connection string.
    /// Connection string containing user name and password, or integrated auth are perfect examples for this.
    /// </summary>
    public class DefaultSqlConnection : ISqlConnection
    {
        private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;
        private readonly SqlServerTransientFaultRetryPolicyConfiguration _transientFaultRetryPolicyConfiguration;

        public DefaultSqlConnection(
            ISqlConnectionStringProvider sqlConnectionStringProvider,
            IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));

            _sqlConnectionStringProvider = sqlConnectionStringProvider;
            _transientFaultRetryPolicyConfiguration = sqlServerDataStoreConfiguration.Value.TransientFaultRetryPolicy;
        }

        /// <inheritdoc />
        public async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
        {
            return await SqlConnectionBuilder.GetBaseSqlConnectionAsync(
                _sqlConnectionStringProvider,
                _transientFaultRetryPolicyConfiguration,
                initialCatalog,
                cancellationToken);
        }
    }
}
