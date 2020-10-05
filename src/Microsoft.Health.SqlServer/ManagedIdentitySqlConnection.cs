// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer
{
    public class ManagedIdentitySqlConnection : ISqlConnection
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly IAccessTokenHandler _accessTokenHandler;

        public ManagedIdentitySqlConnection(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration, IAccessTokenHandler accessTokenHandler)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(accessTokenHandler, nameof(accessTokenHandler));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _accessTokenHandler = accessTokenHandler;
        }

        /// <summary>
        /// Get sql connection after getting access token.
        /// </summary>
        /// <param name="connectToMaster">Should connect to master?</param>
        /// <returns>Sql connection task.</returns>
        public async Task<SqlConnection> GetSqlConnectionAsync(bool connectToMaster = false)
        {
            EnsureArg.IsNotNullOrEmpty(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnectionStringBuilder connectionBuilder = connectToMaster ?
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = string.Empty } :
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnection sqlConnection = new SqlConnection(connectionBuilder.ToString());

            var result = await _accessTokenHandler.GetAccessTokenAsync();
            sqlConnection.AccessToken = result;

            return sqlConnection;
        }

        /// <inheritdoc />
        public SqlConnection GetSqlConnection(bool connectToMaster = false)
        {
            return GetSqlConnectionAsync(connectToMaster).Result;
        }
    }
}
