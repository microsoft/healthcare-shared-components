// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration
{
    public abstract class SqlIntegrationTestBase : IAsyncLifetime
    {
        public SqlIntegrationTestBase(ITestOutputHelper outputHelper)
        {
            DatabaseName = $"IntegrationTests_BaseSchemaRunner_{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
            Config = new SqlServerDataStoreConfiguration
            {
                ConnectionString = $"server=(local);Initial Catalog={DatabaseName};Integrated Security=true",
            };

            ConnectionStringProvider = Substitute.For<ISqlConnectionStringProvider>();
            ConnectionStringProvider.GetSqlConnectionString(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Config.ConnectionString);
        }

        protected string DatabaseName { get; set; }

        protected ISqlConnectionStringProvider ConnectionStringProvider { get; set; }

        protected ITestOutputHelper Output { get; set; }

        protected SqlConnection Connection { get; set; }

        protected SqlServerDataStoreConfiguration Config { get; set; }

        public async Task InitializeAsync()
        {
            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(Config.ConnectionString);
            connectionBuilder.InitialCatalog = "master";
            Connection = new SqlConnection(connectionBuilder.ToString());
            await Connection.OpenAsync();
            await SchemaInitializer.CreateDatabaseAsync(Connection, DatabaseName, CancellationToken.None);
            await Connection.ChangeDatabaseAsync(DatabaseName);
        }

        public async Task DisposeAsync()
        {
            await Connection.ChangeDatabaseAsync("master");
            using (var canCreateDatabaseCommand = new SqlCommand($"ALTER DATABASE {DatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {DatabaseName};", Connection))
            {
                int result = await canCreateDatabaseCommand.ExecuteNonQueryAsync(CancellationToken.None);
                if (result > 0)
                {
                    Output.WriteLine($"Clean up of {DatabaseName} failed with result code {result}");
                    Assert.False(true);
                }
            }

            await Connection.CloseAsync();
        }
    }
}
