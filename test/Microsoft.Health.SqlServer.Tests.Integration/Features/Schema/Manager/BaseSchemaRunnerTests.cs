// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema.Manager;

public sealed class BaseSchemaRunnerTests : SqlIntegrationTestBase, IDisposable
{
    private readonly BaseSchemaRunner _runner;
    private readonly SchemaManagerDataStore _dataStore;
    private readonly SqlTransactionHandler _sqlTransactionHandler = new SqlTransactionHandler();

    public BaseSchemaRunnerTests(ITestOutputHelper output)
        : base(output)
    {
        IOptionsMonitor<SqlServerDataStoreConfiguration> monitor = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        monitor.Get(Arg.Any<string>()).Returns(Config);

        var sqlConnection = new DefaultSqlConnectionBuilder(monitor, SqlConfigurableRetryFactory.CreateNoneRetryProvider());
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(new SqlClientRetryOptions().Settings);

        var sqlConnectionWrapperFactory = new SqlConnectionWrapperFactory(_sqlTransactionHandler, sqlConnection, sqlRetryLogicBaseProvider, monitor);
        _dataStore = new SchemaManagerDataStore(sqlConnectionWrapperFactory, monitor, NullLogger<SchemaManagerDataStore>.Instance);

        _runner = new BaseSchemaRunner(sqlConnectionWrapperFactory, _dataStore, NullLogger<BaseSchemaRunner>.Instance);
    }

    [Fact]
    public async Task EnsureBaseSchemaExist_DoesNotExist_CreatesIt()
    {
        Assert.False(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
        Assert.True(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task EnsureBaseSchemaExist_Exists_DoesNothing()
    {
        Assert.False(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
        Assert.True(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
        Assert.True(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task EnsureInstanceSchemaRecordExists_WhenNotExists_Throws()
    {
        await Assert.ThrowsAsync<SchemaManagerException>(() => _runner.EnsureInstanceSchemaRecordExistsAsync(CancellationToken.None));
    }

    public void Dispose()
    {
        _sqlTransactionHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
