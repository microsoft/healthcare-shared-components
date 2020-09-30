// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features
{
    public class DefaultSqlConnectionFactoryTests
    {
        private const string DatabaseName = "Dicom";
        private const string ServerName = "(local)";

        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly DefaultSqlConnectionFactory _sqlConnectionFactory;

        public DefaultSqlConnectionFactoryTests()
        {
            _sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
            _sqlServerDataStoreConfiguration.ConnectionString = $"server={ServerName};Initial Catalog={DatabaseName};Integrated Security=true";
            _sqlConnectionFactory = new DefaultSqlConnectionFactory(_sqlServerDataStoreConfiguration);
        }

        [Fact]
        public void GivenDefaultConnectionType_WhenSqlConnectionRequested_AccessTokenIsNotSet()
        {
            SqlConnection sqlConnection = _sqlConnectionFactory.GetSqlConnection();

            Assert.Null(sqlConnection.AccessToken);
        }

        [Fact]
        public void GivenDefaultConnectionType_WhenSqlConnectionRequested_DatabaseIsSet()
        {
            SqlConnection sqlConnection = _sqlConnectionFactory.GetSqlConnection();

            Assert.Equal(DatabaseName, sqlConnection.Database);
        }

        [Fact]
        public void GivenDefaultConnectionType_WhenSqlConnectionToMasterRequested_MasterDatabaseIsSet()
        {
            SqlConnection sqlConnection = _sqlConnectionFactory.GetSqlConnection(connectToMaster: true);

            Assert.Empty(sqlConnection.Database);
        }
    }
}
