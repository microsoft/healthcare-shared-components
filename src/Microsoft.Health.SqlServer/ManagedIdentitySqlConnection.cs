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
    public class ManagedIdentitySqlConnection : ISqlConnectionFactory
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly IAccessTokenHandler _accessTokenHandler;
        private readonly string _azureResource = "https://database.windows.net/";

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
        /// <param name="initialCatalog">Initial catalog to connect to. If value is null, or no argument is provided, initial catalog is determined from the connection string.</param>
        /// <returns>Sql connection task.</returns>
        private async Task<SqlConnection> GetSqlConnectionImplAsync(string initialCatalog = null)
        {
            EnsureArg.IsNotNullOrEmpty(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnection sqlConnection;

            if (initialCatalog == null)
            {
                sqlConnection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString);
            }
            else
            {
                SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = initialCatalog };
                sqlConnection = new SqlConnection(connectionBuilder.ToString());
            }

            var result = await _accessTokenHandler.GetAccessTokenAsync(_azureResource);
            sqlConnection.AccessToken = result;

            return sqlConnection;
        }

        /// <inheritdoc />
        public async Task<SqlConnection> GetSqlConnectionAsync()
        {
            return await GetSqlConnectionImplAsync();
        }

        /// <inheritdoc />
        public async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog)
        {
            return await GetSqlConnectionImplAsync(initialCatalog);
        }
    }
}
