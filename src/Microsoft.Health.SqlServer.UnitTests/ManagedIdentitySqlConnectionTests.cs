// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests
{
    public class ManagedIdentitySqlConnectionTests
    {
        private const string DatabaseName = "Dicom";
        private const string ServerName = "(local)";

        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly ManagedIdentitySqlConnection _sqlConnection;

        public ManagedIdentitySqlConnectionTests()
        {
            _sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
            _sqlServerDataStoreConfiguration.ConnectionString = $"Server={ServerName};Database={DatabaseName};";
            _sqlServerDataStoreConfiguration.ConnectionType = SqlServerConnectionType.ManagedIdentity;
            _sqlConnection = new ManagedIdentitySqlConnection(_sqlServerDataStoreConfiguration);
        }

        [Fact]
        public void GivenManagedIdentityConnectionType_WhenSqlConnectionRequested_AccessTokenIsSet()
        {
            SqlConnection sqlConnection = _sqlConnection.GetSqlConnection();

            Assert.NotNull(sqlConnection.AccessToken);
        }

        [Fact]
        public void GivenDefaultConnectionType_WhenSqlConnectionRequested_DatabaseIsSet()
        {
            SqlConnection sqlConnection = _sqlConnection.GetSqlConnection();

            Assert.Equal(DatabaseName, sqlConnection.Database);
        }

        [Fact]
        public void GivenDefaultConnectionType_WhenSqlConnectionToMasterRequested_MasterDatabaseIsSet()
        {
            SqlConnection sqlConnection = _sqlConnection.GetSqlConnection(connectToMaster: true);

            Assert.Empty(sqlConnection.Database);
        }
    }
}
