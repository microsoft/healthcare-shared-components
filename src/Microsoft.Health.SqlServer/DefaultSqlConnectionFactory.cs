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
    /// <summary>
    /// Default Sql Connection Factory is responsible to handle Sql connections that can be made purely based on connection string.
    /// Connection string containing user name and password, or integrated auth are perfect examples for this.
    /// </summary>
    public class DefaultSqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

        public DefaultSqlConnectionFactory(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
        }

        /// <inheritdoc />
        public Task<SqlConnection> GetSqlConnectionAsync()
        {
            return GetSqlConnectionAsync(null);
        }

        /// <inheritdoc />
        public Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog)
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

            return Task.FromResult(sqlConnection);
        }
    }
}
