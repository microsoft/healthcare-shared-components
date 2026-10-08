// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema.Manager;

[TestClass]
public sealed class BaseSchemaRunnerTests : SqlIntegrationTestBase
{
    private readonly BaseSchemaRunner _runner;
    private readonly SchemaManagerDataStore _dataStore;
    private readonly SqlTransactionHandler _sqlTransactionHandler = new SqlTransactionHandler();

    public BaseSchemaRunnerTests(TestContext testContext)
        : base(testContext)
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration());
        var sqlConnection = new DefaultSqlConnectionBuilder(Options.Create(Config), SqlConfigurableRetryFactory.CreateNoneRetryProvider());
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(new SqlClientRetryOptions().Settings);

        var sqlConnectionWrapperFactory = new SqlConnectionWrapperFactory(_sqlTransactionHandler, sqlConnection, sqlRetryLogicBaseProvider, options);
        _dataStore = new SchemaManagerDataStore(sqlConnectionWrapperFactory, options, NullLogger<SchemaManagerDataStore>.Instance);

        ISchemaWriteGate writeGate = Substitute.For<ISchemaWriteGate>();
        writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        _runner = new BaseSchemaRunner(sqlConnectionWrapperFactory, _dataStore, writeGate, NullLogger<BaseSchemaRunner>.Instance);
    }

    [TestMethod]
    public async Task EnsureBaseSchemaExist_DoesNotExist_CreatesIt()
    {
        Assert.IsFalse(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
        Assert.IsTrue(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task EnsureBaseSchemaExist_Exists_DoesNothing()
    {
        Assert.IsFalse(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
        Assert.IsTrue(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
        Assert.IsTrue(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task EnsureInstanceSchemaRecordExists_WhenNotExists_Throws()
    {
        await Assert.ThrowsAsync<SchemaManagerException>(() => _runner.EnsureInstanceSchemaRecordExistsAsync(CancellationToken.None));
    }

    public override ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _sqlTransactionHandler.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
