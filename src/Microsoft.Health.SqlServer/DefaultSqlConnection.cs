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
        public SqlConnection GetSqlConnection(bool connectToMaster = false)
        {
            EnsureArg.IsNotNullOrEmpty(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnectionStringBuilder connectionBuilder = connectToMaster ?
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = string.Empty } :
                new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString);

            SqlConnection sqlConnection = new SqlConnection(connectionBuilder.ToString());
            return sqlConnection;
        }
    }
}
