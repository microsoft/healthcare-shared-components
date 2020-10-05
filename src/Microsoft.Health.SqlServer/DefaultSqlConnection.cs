// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer
{
    /// <summary>
    /// Default Sql Connection Factory is responsible to handle Sql connections that can be made purely based on connection string.
    /// Connection string containing user name and password, or integrated auth are perfect examples for this.
    /// </summary>
    public class DefaultSqlConnection : ISqlConnection
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

        public DefaultSqlConnection(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
        }

        /// <inheritdoc />
        public SqlConnection GetSqlConnection()
        {
            return GetSqlConnection(null);
        }

        /// <inheritdoc />
        public SqlConnection GetSqlConnection(string initialCatalog)
        {
            EnsureArg.IsNotNullOrEmpty(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnectionStringBuilder connectionBuilder = initialCatalog == null ?
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) :
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = initialCatalog };

            SqlConnection sqlConnection = new SqlConnection(connectionBuilder.ToString());
            return sqlConnection;
        }
    }
}
