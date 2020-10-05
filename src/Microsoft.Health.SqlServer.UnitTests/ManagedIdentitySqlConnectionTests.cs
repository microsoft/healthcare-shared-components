// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests
{
    public class ManagedIdentitySqlConnectionTests
    {
        private const string DatabaseName = "Dicom";
        private const string ServerName = "(local)";
        private const string MasterDatabase = "master";
        private const string TestAccessToken = "test token";

        private readonly ManagedIdentitySqlConnection _sqlConnection;

        public ManagedIdentitySqlConnectionTests()
        {
            var accessTokenHandler = Substitute.For<IAccessTokenHandler>();
            accessTokenHandler.GetAccessTokenAsync(string.Empty).Returns(Task.FromResult(TestAccessToken));

            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
            sqlServerDataStoreConfiguration.ConnectionString = $"Server={ServerName};Database={DatabaseName};";
            sqlServerDataStoreConfiguration.ConnectionType = SqlServerAuthenticationType.ManagedIdentity;
            _sqlConnection = new ManagedIdentitySqlConnection(sqlServerDataStoreConfiguration, accessTokenHandler);
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
            SqlConnection sqlConnection = _sqlConnection.GetSqlConnection(MasterDatabase);

            Assert.Equal(MasterDatabase, sqlConnection.Database);
        }
    }
}
