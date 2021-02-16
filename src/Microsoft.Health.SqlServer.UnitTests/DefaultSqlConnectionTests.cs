﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features
{
    public class DefaultSqlConnectionTests
    {
        private const string DatabaseName = "Dicom";
        private const string ServerName = "(local)";
        private const string MasterDatabase = "master";

        private readonly DefaultSqlConnectionFactory _sqlConnectionFactory;

        public DefaultSqlConnectionTests()
        {
            var sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
            sqlServerDataStoreConfiguration.ConnectionString = $"server={ServerName};Initial Catalog={DatabaseName};Integrated Security=true";
            _sqlConnectionFactory = new DefaultSqlConnectionFactory(new DefaultSqlConnectionStringProvider(sqlServerDataStoreConfiguration));
        }

        [Fact]
        public async Task GivenDefaultConnectionType_WhenSqlConnectionRequested_AccessTokenIsNotSet()
        {
            SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync();

            Assert.Null(sqlConnection.AccessToken);
        }

        [Fact]
        public async Task GivenDefaultConnectionType_WhenSqlConnectionRequested_DatabaseIsSet()
        {
            SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync();

            Assert.Equal(DatabaseName, sqlConnection.Database);
        }

        [Fact]
        public async Task GivenDefaultConnectionType_WhenSqlConnectionToMasterRequested_MasterDatabaseIsSet()
        {
            SqlConnection sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync(MasterDatabase);

            Assert.Equal(MasterDatabase, sqlConnection.Database);
        }
    }
}
