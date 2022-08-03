// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration;

public abstract class SqlIntegrationTestBase : IAsyncLifetime
{
    protected SqlIntegrationTestBase(ITestOutputHelper outputHelper)
    {
        Output = outputHelper;
        DatabaseName = $"IntegrationTests_BaseSchemaRunner_{Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.Ordinal)}";
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TestSqlConnectionString") ?? $"server=(local);Integrated Security=true;TrustServerCertificate=true;")
        {
            InitialCatalog = DatabaseName
        };

        Config = new SqlServerDataStoreConfiguration
        {
            ConnectionString = builder.ToString(),
            AllowDatabaseCreation = true,
        };

        ConnectionStringProvider = Substitute.For<ISqlConnectionStringProvider>();
        ConnectionStringProvider.GetSqlConnectionString(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Config.ConnectionString);
    }

    protected string DatabaseName { get; set; }

    protected ISqlConnectionStringProvider ConnectionStringProvider { get; set; }

    protected ITestOutputHelper Output { get; set; }

    protected SqlTransactionHandler TransactionHandler { get; set; }

    protected SqlConnectionWrapperFactory ConnectionFactory { get; set; }

    protected SqlConnectionWrapper ConnectionWrapper { get; set; }

    protected SqlServerDataStoreConfiguration Config { get; set; }

    public virtual async Task InitializeAsync()
    {
        TransactionHandler = new SqlTransactionHandler();

        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(Config);
        ConnectionFactory = new SqlConnectionWrapperFactory(
            TransactionHandler,
            new DefaultSqlConnectionBuilder(ConnectionStringProvider, SqlConfigurableRetryFactory.CreateNoneRetryProvider()),
            SqlConfigurableRetryFactory.CreateFixedRetryProvider(new SqlClientRetryOptions().Settings),
            options);

        ConnectionWrapper = await ConnectionFactory.ObtainSqlConnectionWrapperAsync("master", CancellationToken.None).ConfigureAwait(false);

        await SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, DatabaseName, CancellationToken.None).ConfigureAwait(false);
        await ConnectionWrapper.SqlConnection.ChangeDatabaseAsync(DatabaseName).ConfigureAwait(false);
        Output.WriteLine($"Using database '{DatabaseName}'.");
    }

    public virtual async Task DisposeAsync()
    {
        await ConnectionWrapper.SqlConnection.ChangeDatabaseAsync("master").ConfigureAwait(false);
        try
        {
            await DeleteDatabaseAsync(DatabaseName).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Output.WriteLine($"Failed to delete test database after test run: {e.Message}{Environment.NewLine}{Environment.NewLine}{e.StackTrace}");
            throw;
        }

        await ConnectionWrapper.SqlConnection.CloseAsync().ConfigureAwait(false);
        ConnectionWrapper.Dispose();
        TransactionHandler.Dispose();
    }

    protected async Task DeleteDatabaseAsync(string dbName)
    {
        if (!Identifier.IsValidDatabase(dbName))
        {
            throw new ArgumentException($"Invalid DB identifier '{dbName}'", nameof(dbName));
        }

        using SqlCommandWrapper deleteDatabaseCommand = ConnectionWrapper.CreateRetrySqlCommand();
        deleteDatabaseCommand.CommandText = $"ALTER DATABASE {dbName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {dbName};";

        if (ConnectionWrapper.SqlConnection.Database == dbName)
        {
            Output.WriteLine($"Switching from '{dbName}' to master prior to delete.");
            await ConnectionWrapper.SqlConnection.ChangeDatabaseAsync("master", CancellationToken.None).ConfigureAwait(false);
        }

        int result = await deleteDatabaseCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        if (result != -1)
        {
            Output.WriteLine($"Clean up of {dbName} failed with result code {result}.");
            Assert.False(true);
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    protected async Task<SqlConnection> GetSqlConnection()
    {
        var connectionBuilder = new SqlConnectionStringBuilder(Config.ConnectionString);
        var result = new SqlConnection(connectionBuilder.ToString());
        await result.OpenAsync().ConfigureAwait(false);
        return result;
    }
}
