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
    public class ManagedIdentitySqlConnection : ISqlConnection
    {
        private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;
        private readonly IAccessTokenHandler _accessTokenHandler;
        private readonly SqlServerTransientFaultRetryPolicyConfiguration _transientFaultRetryPolicyConfiguration;
        private readonly string _azureResource = "https://database.windows.net/";

        public ManagedIdentitySqlConnection(
            ISqlConnectionStringProvider sqlConnectionStringProvider,
            IAccessTokenHandler accessTokenHandler,
            IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));
            EnsureArg.IsNotNull(accessTokenHandler, nameof(accessTokenHandler));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));

            _sqlConnectionStringProvider = sqlConnectionStringProvider;
            _accessTokenHandler = accessTokenHandler;
            _transientFaultRetryPolicyConfiguration = sqlServerDataStoreConfiguration.Value.TransientFaultRetryPolicy;
        }

        /// <inheritdoc />
        public async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
        {
            SqlConnection sqlConnection = await SqlConnectionBuilder.GetBaseSqlConnectionAsync(
                                                                        _sqlConnectionStringProvider,
                                                                        _transientFaultRetryPolicyConfiguration,
                                                                        initialCatalog,
                                                                        cancellationToken);

            // set managed identity access token
            var result = await _accessTokenHandler.GetAccessTokenAsync(_azureResource, cancellationToken);
            sqlConnection.AccessToken = result;
            return sqlConnection;
        }
    }
}
