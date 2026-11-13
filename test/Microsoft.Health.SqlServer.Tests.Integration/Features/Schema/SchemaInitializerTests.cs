// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Medino;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema;

[TestClass]
public class SchemaInitializerTests : SqlIntegrationTestBase
{
    public SchemaInitializerTests(TestContext testContext)
        : base(testContext)
    {
    }

    [TestMethod]
    public async Task InvalidDatabaseName_CreateDatabaseAsync_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, "[something] DROP DATABASE Production --", CancellationToken.None));
    }

    [TestMethod]
    public async Task DatabaseDoesNotExist_DoesDatabaseExistAsync_ReturnsFalse()
    {
        Assert.IsFalse(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, "doesnotexist", CancellationToken.None));
    }

    [TestMethod]
    public async Task DatabaseExists_DoesDatabaseExistAsync_ReturnsTrue()
    {
        string dbName = $"Db_{Guid.NewGuid():N}";

        try
        {
            Assert.IsFalse(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, dbName, CancellationToken.None));
            Assert.IsTrue(await SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, dbName, CancellationToken.None));
            Assert.IsTrue(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, dbName, CancellationToken.None));
        }
        finally
        {
            await DeleteDatabaseAsync(dbName);
        }
    }

    [TestMethod]
    public async Task GivenNewDatabase_WhenInitializingWithScope_ThenAppliesLatestSchemaAndNotifiesWithDatabaseName()
    {
        IServiceProvider rootProvider = Substitute.For<IServiceProvider>();
        IMediator mediator = Substitute.For<IMediator>();
        var schemaInformation = new SchemaInformation((int)SchemaVersion.Version1, (int)SchemaVersion.Version2);
        SchemaInitializer initializer = CreateInitializer(rootProvider, schemaInformation, mediator);

        await initializer.InitializeAsync(CreateScope(), cancellationToken: CancellationToken.None);

        Assert.AreEqual((int)SchemaVersion.Version2, schemaInformation.Current);
        (string, int, bool) notification = Assert.ContainsSingle(GetNotifications(mediator));
        Assert.AreEqual((DatabaseName, (int)SchemaVersion.Version2, true), notification);
        rootProvider.DidNotReceiveWithAnyArgs().GetService(default);
    }

    [TestMethod]
    public async Task GivenNewDatabase_WhenInitializingIncrementallyWithScope_ThenNotifiesEachVersionWithDatabaseName()
    {
        IServiceProvider rootProvider = Substitute.For<IServiceProvider>();
        IMediator mediator = Substitute.For<IMediator>();
        // Use 2 -> 3 because the 2.diff.sql test asset inserts its own SchemaVersion row and cannot be applied incrementally
        var schemaInformation = new SchemaInformation(2, 3);
        SchemaInitializer initializer = CreateInitializer(rootProvider, schemaInformation, mediator);

        await initializer.InitializeAsync(CreateScope(), forceIncrementalSchemaUpgrade: true, CancellationToken.None);

        Assert.AreEqual(3, schemaInformation.Current);
        CollectionAssert.AreEqual(
            new[] { (DatabaseName, 2, true), (DatabaseName, 3, false) },
            GetNotifications(mediator));
        rootProvider.DidNotReceiveWithAnyArgs().GetService(default);
    }

    [TestMethod]
    public async Task GivenUpToDateDatabase_WhenInitializingWithScope_ThenNotifiesCurrentVersionWithDatabaseName()
    {
        IServiceProvider rootProvider = Substitute.For<IServiceProvider>();
        IMediator mediator = Substitute.For<IMediator>();
        var schemaInformation = new SchemaInformation((int)SchemaVersion.Version1, (int)SchemaVersion.Version2);
        SchemaInitializer initializer = CreateInitializer(rootProvider, schemaInformation, mediator);
        await initializer.InitializeAsync(CreateScope(), cancellationToken: CancellationToken.None);
        mediator.ClearReceivedCalls();

        await initializer.InitializeAsync(CreateScope(), cancellationToken: CancellationToken.None);

        (string, int, bool) notification = Assert.ContainsSingle(GetNotifications(mediator));
        Assert.AreEqual((DatabaseName, (int)SchemaVersion.Version2, false), notification);
        rootProvider.DidNotReceiveWithAnyArgs().GetService(default);
    }

    private SchemaInitializer CreateInitializer(IServiceProvider rootProvider, SchemaInformation schemaInformation, IMediator mediator)
    {
        Config.Initialize = true;
        Config.SchemaOptions.AutomaticUpdatesEnabled = true;

        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(Config);
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        var evaluator = new SchemaWriteGateEvaluator(
            gate,
            Substitute.For<ISchemaMetrics>(),
            Substitute.For<ISqlConnectionBuilder>(),
            options,
            NullLogger<SchemaWriteGateEvaluator>.Instance);

        return new SchemaInitializer(rootProvider, options, schemaInformation, mediator, evaluator, NullLogger<SchemaInitializer>.Instance);
    }

    // Wires the scope by hand so the test controls exactly which database the scoped services target
    private IServiceScope CreateScope()
    {
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(Config);
        var schemaManagerDataStore = new SchemaManagerDataStore(ConnectionFactory, options, NullLogger<SchemaManagerDataStore>.Instance);
        var schemaUpgradeRunner = new SchemaUpgradeRunner(
            new ScriptProvider<SchemaVersion>(),
            new BaseScriptProvider(),
            NullLogger<SchemaUpgradeRunner>.Instance,
            ConnectionFactory,
            schemaManagerDataStore);

        IServiceProvider scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider.GetService(typeof(SqlConnectionWrapperFactory)).Returns(ConnectionFactory);
        scopedProvider.GetService(typeof(IReadOnlySchemaManagerDataStore)).Returns(schemaManagerDataStore);
        scopedProvider.GetService(typeof(SchemaUpgradeRunner)).Returns(schemaUpgradeRunner);

        IServiceScope scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);
        return scope;
    }

    private static List<(string DatabaseName, int Version, bool IsFullSchemaSnapshot)> GetNotifications(IMediator mediator)
        => mediator
            .ReceivedCalls()
            .SelectMany(c => c.GetArguments())
            .OfType<SchemaUpgradedNotification>()
            .Select(n => (n.DatabaseName, n.Version, n.IsFullSchemaSnapshot))
            .ToList();
}
