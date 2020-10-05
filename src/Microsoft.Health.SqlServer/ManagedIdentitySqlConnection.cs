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
        private readonly string _azureResource;

        public ManagedIdentitySqlConnection(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration, IAccessTokenHandler accessTokenHandler, string azureResource = "https://database.windows.net/")
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(accessTokenHandler, nameof(accessTokenHandler));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _accessTokenHandler = accessTokenHandler;
            _azureResource = azureResource;
        }

        /// <summary>
        /// Get sql connection after getting access token.
        /// </summary>
        /// <param name="initialCatalog">Initial catalog to connect to. If value is null, or no argument is provided, initial catalog is determined from the connection string.</param>
        /// <returns>Sql connection task.</returns>
        private async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null)
        {
            EnsureArg.IsNotNullOrEmpty(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnectionStringBuilder connectionBuilder = initialCatalog == null ?
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) :
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = initialCatalog };

            SqlConnection sqlConnection = new SqlConnection(connectionBuilder.ToString());

            var result = await _accessTokenHandler.GetAccessTokenAsync(_azureResource);
            sqlConnection.AccessToken = result;

            return sqlConnection;
        }

        /// <inheritdoc />
        public SqlConnection GetSqlConnection()
        {
            return GetSqlConnectionAsync().Result;
        }

        /// <inheritdoc />
        public SqlConnection GetSqlConnection(string initialCatalog)
        {
            return GetSqlConnectionAsync(initialCatalog).Result;
        }
    }
}
