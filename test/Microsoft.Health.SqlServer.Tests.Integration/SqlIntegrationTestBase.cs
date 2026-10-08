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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.Tests.Integration;

public abstract class SqlIntegrationTestBase : IAsyncDisposable
{
    private readonly TestContext _testContext;
    private bool _disposed;

    protected SqlIntegrationTestBase(TestContext testContext)
    {
        _testContext = testContext;
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
    }

    protected string DatabaseName { get; set; }

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
            new DefaultSqlConnectionBuilder(Options.Create(Config), SqlConfigurableRetryFactory.CreateNoneRetryProvider()),
            SqlConfigurableRetryFactory.CreateFixedRetryProvider(new SqlClientRetryOptions().Settings),
            options);

        ConnectionWrapper = await ConnectionFactory.ObtainSqlConnectionWrapperAsync("master", CancellationToken.None).ConfigureAwait(false);

        await SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, DatabaseName, CancellationToken.None).ConfigureAwait(false);
        await ConnectionWrapper.SqlConnection.ChangeDatabaseAsync(DatabaseName).ConfigureAwait(false);
        _testContext.WriteLine($"Using database '{DatabaseName}'.");
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(disposing: true);
        GC.SuppressFinalize(this);
    }

    public virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                await ConnectionWrapper.SqlConnection.ChangeDatabaseAsync("master").ConfigureAwait(false);
                try
                {
                    await DeleteDatabaseAsync(DatabaseName).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _testContext.WriteLine($"Failed to delete test database after test run: {e.Message}{Environment.NewLine}{Environment.NewLine}{e.StackTrace}");
                    throw;
                }

                await ConnectionWrapper.SqlConnection.CloseAsync().ConfigureAwait(false);
                ConnectionWrapper.Dispose();
                TransactionHandler.Dispose();
            }

            _disposed = true;
        }
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
            _testContext.WriteLine($"Switching from '{dbName}' to master prior to delete.");
            await ConnectionWrapper.SqlConnection.ChangeDatabaseAsync("master", CancellationToken.None).ConfigureAwait(false);
        }

        int result = await deleteDatabaseCommand.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        if (result != -1)
        {
            Assert.Fail($"Clean up of {dbName} failed with result code {result}.");
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    protected SqlConnection GetSqlConnection()
    {
        var connectionBuilder = new SqlConnectionStringBuilder(Config.ConnectionString);
        return new SqlConnection(connectionBuilder.ToString());
    }
}
