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

namespace Microsoft.Health.SqlServer.Tests.Integration;

public abstract class SqlIntegrationTestBase : IAsyncLifetime
{
    public SqlIntegrationTestBase(ITestOutputHelper outputHelper)
    {
        Output = outputHelper;
        DatabaseName = $"IntegrationTests_BaseSchemaRunner_{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
        Config = new SqlServerDataStoreConfiguration
        {
            ConnectionString = Environment.GetEnvironmentVariable("TestSqlConnectionString") ?? $"server=(local);Initial Catalog={DatabaseName};Integrated Security=true",
            AllowDatabaseCreation = true,
        };

        ConnectionStringProvider = Substitute.For<ISqlConnectionStringProvider>();
        ConnectionStringProvider.GetSqlConnectionString(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Config.ConnectionString);
    }

    protected string DatabaseName { get; set; }

    protected ISqlConnectionStringProvider ConnectionStringProvider { get; set; }

    protected ITestOutputHelper Output { get; set; }

    protected SqlConnection Connection { get; set; }

    protected SqlServerDataStoreConfiguration Config { get; set; }

    public virtual async Task InitializeAsync()
    {
        var connectionBuilder = new SqlConnectionStringBuilder(Config.ConnectionString) { InitialCatalog = "master" };
        Connection = new SqlConnection(connectionBuilder.ToString());
        await Connection.OpenAsync();
        await SchemaInitializer.CreateDatabaseAsync(Connection, DatabaseName, CancellationToken.None);
        await Connection.ChangeDatabaseAsync(DatabaseName);
        Output.WriteLine($"Using database '{DatabaseName}'.");
    }

    public virtual async Task DisposeAsync()
    {
        await Connection.ChangeDatabaseAsync("master");
        try
        {
            await DeleteDatabaseAsync(DatabaseName);
        }
        catch (Exception e)
        {
            Output.WriteLine($"Failed to delete test database after test run: {e.Message}{Environment.NewLine}{Environment.NewLine}{e.StackTrace}");
            throw;
        }

        await Connection.CloseAsync();
        await Connection.DisposeAsync();
    }

    protected async Task DeleteDatabaseAsync(string dbName)
    {
        using var deleteDatabaseCommand = new SqlCommand($"ALTER DATABASE {dbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {dbName};", Connection);
        if (Connection.Database == dbName)
        {
            Output.WriteLine($"Switching from '{dbName}' to master prior to delete.");
            await Connection.ChangeDatabaseAsync("master", CancellationToken.None);
        }

        int result = await deleteDatabaseCommand.ExecuteNonQueryAsync(CancellationToken.None);
        if (result != -1)
        {
            Output.WriteLine($"Clean up of {dbName} failed with result code {result}.");
            Assert.False(true);
        }
    }

    protected async Task<SqlConnection> GetSqlConnection()
    {
        var connectionBuilder = new SqlConnectionStringBuilder(Config.ConnectionString);
        var result = new SqlConnection(connectionBuilder.ToString());
        await result.OpenAsync();
        return result;
    }
}
