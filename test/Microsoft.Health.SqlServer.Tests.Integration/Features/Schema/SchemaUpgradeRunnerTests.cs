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
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.SqlServer.Management.Common;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema;

public sealed class SchemaUpgradeRunnerTests : SqlIntegrationTestBase, IDisposable
{
    private SchemaUpgradeRunner _runner;
    private SchemaManagerDataStore _schemaDataStore;
    private readonly SqlTransactionHandler _sqlTransactionHandler = new SqlTransactionHandler();

    public SchemaUpgradeRunnerTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);

        var sqlConnection = Substitute.For<ISqlConnectionBuilder>();
        sqlConnection.GetSqlConnectionAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs((x) => GetSqlConnection());
        var config = Options.Create(new SqlServerDataStoreConfiguration());

        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(new SqlClientRetryOptions().Settings);
        var sqlConnectionWrapperFactory = new SqlConnectionWrapperFactory(_sqlTransactionHandler, sqlConnection, sqlRetryLogicBaseProvider, config);

        _schemaDataStore = new SchemaManagerDataStore(sqlConnectionWrapperFactory, config, NullLogger<SchemaManagerDataStore>.Instance);
        _runner = new SchemaUpgradeRunner(new ScriptProvider<SchemaVersion>(), new BaseScriptProvider(), NullLogger<SchemaUpgradeRunner>.Instance, sqlConnectionWrapperFactory, _schemaDataStore);
    }

    [Fact]
    public async Task ApplyBaseSchema_DoesNotExist_Succeeds()
    {
        Assert.False(await _schemaDataStore.BaseSchemaExistsAsync(CancellationToken.None).ConfigureAwait(false));
        await _runner.ApplyBaseSchemaAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.True(await _schemaDataStore.BaseSchemaExistsAsync(CancellationToken.None).ConfigureAwait(false));
    }

    [Fact]
    public async Task ApplySchema_BaseSchemaDoesNotExist_Fails()
    {
        Assert.False(await _schemaDataStore.BaseSchemaExistsAsync(CancellationToken.None).ConfigureAwait(false));
        var outerException = await Assert.ThrowsAsync<SqlException>(() => _runner.ApplySchemaAsync(1, true, CancellationToken.None)).ConfigureAwait(false);
        Assert.Contains("Could not find stored procedure 'dbo.UpsertSchemaVersion'.", outerException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplySchema_BaseSchemaExists_Succeeds()
    {
        await _runner.ApplyBaseSchemaAsync(CancellationToken.None).ConfigureAwait(false);
        await _runner.ApplySchemaAsync(1, applyFullSchemaSnapshot: true, CancellationToken.None).ConfigureAwait(false);
        var version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(1, version);
    }

    [Fact]
    public async Task ApplySchema_UsingDiff_Succeeds()
    {
        await _runner.ApplyBaseSchemaAsync(CancellationToken.None).ConfigureAwait(false);
        await _runner.ApplySchemaAsync(2, applyFullSchemaSnapshot: true, CancellationToken.None).ConfigureAwait(false);
        var version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(2, version);
        await _runner.ApplySchemaAsync(3, applyFullSchemaSnapshot: false, CancellationToken.None).ConfigureAwait(false);
        version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(3, version);
    }

    [Fact]
    public async Task ApplyFullSchemaAndDiffScript_OnPreviouslyFailedAttempt_Succeeds()
    {
        await _runner.ApplyBaseSchemaAsync(CancellationToken.None).ConfigureAwait(false);

        // this is to generate an error
        await _schemaDataStore.ExecuteScriptAsync("Insert into SchemaVersion values (2, 'started')", CancellationToken.None).ConfigureAwait(false);

        // attempt 1 : To apply schemaVersion-2 fails
        await Assert.ThrowsAsync<ExecutionFailureException>(() => _runner.ApplySchemaAsync(2, applyFullSchemaSnapshot: true, CancellationToken.None)).ConfigureAwait(false);

        // attempt 2 : To apply schemaVersion-2 passes
        await _runner.ApplySchemaAsync(2, applyFullSchemaSnapshot: true, CancellationToken.None).ConfigureAwait(false);
        var version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(2, version);

        // diff script for version 3 should pass even if SchemaVersion table has an entry with 'failed' status for version 3
        await _schemaDataStore.ExecuteScriptAsync("Insert into SchemaVersion values (3, 'failed')", CancellationToken.None).ConfigureAwait(false);
        await _runner.ApplySchemaAsync(3, applyFullSchemaSnapshot: false, CancellationToken.None).ConfigureAwait(false);
        version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(3, version);
    }

    public void Dispose()
    {
        _sqlTransactionHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
