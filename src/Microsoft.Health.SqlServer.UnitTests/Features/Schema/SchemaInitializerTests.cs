// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Medino;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

[TestClass]
public sealed class SchemaInitializerTests
{
    [TestMethod]
    [DataRow(null, 5, (int)SecondarySchemaStatus.Unknown)]
    [DataRow(3, 5, (int)SecondarySchemaStatus.Behind)]
    [DataRow(1, 2, (int)SecondarySchemaStatus.Behind)]
    [DataRow(5, 5, (int)SecondarySchemaStatus.Current)]
    [DataRow(7, 5, (int)SecondarySchemaStatus.Ahead)]
    public void GivenSchemaVersions_WhenGettingSecondarySchemaStatus_ReturnsExpectedStatus(int? currentVersion, int maximumSupportedVersion, int expectedStatus)
    {
        Assert.AreEqual(expectedStatus, (int)SchemaWriteGateEvaluator.GetSecondarySchemaStatus(currentVersion, maximumSupportedVersion));
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalse_WhenCheckingCanApplySchemaUpdates_ReturnsFalseAndConsultsGate()
    {
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(false));
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, NullLogger<SchemaWriteGateEvaluator>.Instance, gate: gate);

        bool result = await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        Assert.IsFalse(result);
        await gate.ReceivedWithAnyArgs(1).CanWriteAsync(default);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsTrue_WhenCheckingCanApplySchemaUpdates_ReturnsTrue()
    {
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, NullLogger<SchemaWriteGateEvaluator>.Instance, gate: gate);

        bool result = await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalseAndSchemaBehind_WhenCheckingCanApplySchemaUpdates_LogsBehind()
    {
        var logger = new ListLogger<SchemaWriteGateEvaluator>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, logger);

        await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        (LogLevel Level, string Message) entry = Assert.ContainsSingle(logger.Entries);
        Assert.AreEqual(LogLevel.Information, entry.Level);
        Assert.Contains("Schema status: Behind; current version: 3; latest supported version: 5", entry.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalseAndSchemaCurrent_WhenCheckingCanApplySchemaUpdates_LogsCurrent()
    {
        var logger = new ListLogger<SchemaWriteGateEvaluator>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 5 }, logger);

        await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        (LogLevel Level, string Message) entry = Assert.ContainsSingle(logger.Entries);
        Assert.AreEqual(LogLevel.Information, entry.Level);
        Assert.Contains("Schema status: Current; current version: 5; latest supported version: 5", entry.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalseAndSchemaAhead_WhenCheckingCanApplySchemaUpdates_LogsWarning()
    {
        var logger = new ListLogger<SchemaWriteGateEvaluator>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 7 }, logger);

        await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        (LogLevel Level, string Message) entry = Assert.ContainsSingle(logger.Entries);
        Assert.AreEqual(LogLevel.Warning, entry.Level);
        Assert.Contains("Schema status: Ahead; current version: 7; latest supported version: 5", entry.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalseAndVersionUnknown_WhenCheckingCanApplySchemaUpdates_LogsWarning()
    {
        var logger = new ListLogger<SchemaWriteGateEvaluator>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = null }, logger);

        await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        (LogLevel Level, string Message) entry = Assert.ContainsSingle(logger.Entries);
        Assert.AreEqual(LogLevel.Warning, entry.Level);
        Assert.Contains("Schema status: Unknown; current version: (null); latest supported version: 5", entry.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalseAndSchemaBehind_WhenCheckingCanApplySchemaUpdates_EmitsSchemaBehindMetric()
    {
        ISchemaMetrics metrics = Substitute.For<ISchemaMetrics>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, NullLogger<SchemaWriteGateEvaluator>.Instance, metrics, region: "eastus2", databaseName: "MyDatabase");

        await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        metrics.Received(1).SchemaBehind("MyDatabase", 3, "eastus2");
    }

    [TestMethod]
    [DataRow(5)] // current
    [DataRow(7)] // ahead
    [DataRow(null)] // unknown
    public async Task GivenWriteGateReturnsFalseAndSchemaNotBehind_WhenCheckingCanApplySchemaUpdates_DoesNotEmitMetric(int? currentVersion)
    {
        ISchemaMetrics metrics = Substitute.For<ISchemaMetrics>();
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = currentVersion }, NullLogger<SchemaWriteGateEvaluator>.Instance, metrics);

        await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        metrics.DidNotReceiveWithAnyArgs().SchemaBehind(default, default, default);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsTrue_WhenCheckingCanApplySchemaUpdates_DoesNotEmitMetric()
    {
        ISchemaMetrics metrics = Substitute.For<ISchemaMetrics>();
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5) { Current = 3 }, NullLogger<SchemaWriteGateEvaluator>.Instance, metrics, gate: gate);

        await initializer.CanApplySchemaUpdatesAsync(CancellationToken.None);

        metrics.DidNotReceiveWithAnyArgs().SchemaBehind(default, default, default);
    }

    private static ISchemaWriteGate FalseGate()
    {
        ISchemaWriteGate gate = Substitute.For<ISchemaWriteGate>();
        gate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(false));
        return gate;
    }

    [TestMethod]
    public async Task GivenNullScope_WhenInitializing_ThenThrowsArgumentNullException()
    {
        SchemaInitializer initializer = CreateInitializer(new SchemaInformation(1, 5), NullLogger<SchemaWriteGateEvaluator>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => initializer.InitializeAsync(null, cancellationToken: CancellationToken.None));
    }

    [TestMethod]
    public async Task GivenNoConnectionString_WhenInitializing_ThenSkipsWithoutUsingScopeOrPublishing()
    {
        IServiceProvider rootProvider = Substitute.For<IServiceProvider>();
        IMediator mediator = Substitute.For<IMediator>();
        IServiceScope scope = Substitute.For<IServiceScope>();
        SchemaInitializer initializer = CreateInitializer(rootProvider, new SqlServerDataStoreConfiguration { ConnectionString = null }, mediator);

        await initializer.InitializeAsync(scope, cancellationToken: CancellationToken.None);

        _ = scope.DidNotReceive().ServiceProvider;
        rootProvider.DidNotReceiveWithAnyArgs().GetService(default);
        Assert.IsEmpty(mediator.ReceivedCalls());
    }

    [TestMethod]
    public async Task GivenScope_WhenInitializing_ThenResolvesConnectionFactoryFromProvidedScope()
    {
        IServiceProvider rootProvider = Substitute.For<IServiceProvider>();
        IMediator mediator = Substitute.For<IMediator>();
        IServiceProvider scopedProvider = Substitute.For<IServiceProvider>();
        using var transactionHandler = new SqlTransactionHandler();
        SqlConnectionWrapperFactory connectionFactory = CreateConnectionWrapperFactory(transactionHandler, "ScopedDb");
        scopedProvider.GetService(typeof(SqlConnectionWrapperFactory)).Returns(connectionFactory);
        IServiceScope scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);

        // Initialize = false stops processing right after the connection factory is resolved
        SchemaInitializer initializer = CreateInitializer(
            rootProvider,
            new SqlServerDataStoreConfiguration { ConnectionString = "Server=(local);Database=ScopedDb;", Initialize = false },
            mediator);

        await initializer.InitializeAsync(scope, cancellationToken: CancellationToken.None);

        scopedProvider.Received(1).GetService(typeof(SqlConnectionWrapperFactory));
        rootProvider.DidNotReceiveWithAnyArgs().GetService(default);
        scope.DidNotReceive().Dispose();
        Assert.IsEmpty(mediator.ReceivedCalls());
    }

    [TestMethod]
    public async Task GivenHostedService_WhenStarting_ThenCreatesAndDisposesScope()
    {
        IServiceScope scope = Substitute.For<IServiceScope>();
        IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        IServiceProvider rootProvider = Substitute.For<IServiceProvider>();
        rootProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        IHostedService initializer = CreateInitializer(rootProvider, new SqlServerDataStoreConfiguration { ConnectionString = null }, Substitute.For<IMediator>());

        await initializer.StartAsync(CancellationToken.None);

        scopeFactory.Received(1).CreateScope();
        scope.Received(1).Dispose();
    }

    private static SqlConnectionWrapperFactory CreateConnectionWrapperFactory(SqlTransactionHandler transactionHandler, string databaseName)
    {
        ISqlConnectionBuilder connectionBuilder = Substitute.For<ISqlConnectionBuilder>();
        connectionBuilder.DefaultDatabase.Returns(databaseName);
        return new SqlConnectionWrapperFactory(
            transactionHandler,
            connectionBuilder,
            SqlConfigurableRetryFactory.CreateNoneRetryProvider(),
            Options.Create(new SqlServerDataStoreConfiguration()));
    }

    private static SchemaInitializer CreateInitializer(IServiceProvider serviceProvider, SqlServerDataStoreConfiguration configuration, IMediator mediator)
    {
        var options = Options.Create(configuration);
        var evaluator = new SchemaWriteGateEvaluator(
            FalseGate(),
            Substitute.For<ISchemaMetrics>(),
            Substitute.For<ISqlConnectionBuilder>(),
            options,
            NullLogger<SchemaWriteGateEvaluator>.Instance);

        return new SchemaInitializer(
            serviceProvider,
            options,
            new SchemaInformation(1, 5),
            mediator,
            evaluator,
            NullLogger<SchemaInitializer>.Instance);
    }

    private static SchemaInitializer CreateInitializer(
        SchemaInformation schemaInformation,
        ILogger<SchemaWriteGateEvaluator> logger,
        ISchemaMetrics schemaMetrics = null,
        string region = null,
        string databaseName = "testdb",
        ISchemaWriteGate gate = null)
    {
        ISqlConnectionBuilder connectionBuilder = Substitute.For<ISqlConnectionBuilder>();
        connectionBuilder.DefaultDatabase.Returns(databaseName);
        var options = Options.Create(new SqlServerDataStoreConfiguration { Region = region });
        var evaluator = new SchemaWriteGateEvaluator(gate ?? FalseGate(), schemaMetrics ?? Substitute.For<ISchemaMetrics>(), connectionBuilder, options, logger);
        return new SchemaInitializer(
            Substitute.For<IServiceProvider>(),
            options,
            schemaInformation,
            Substitute.For<IMediator>(),
            evaluator,
            NullLogger<SchemaInitializer>.Instance);
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new List<(LogLevel Level, string Message)>();

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            public void Dispose()
            {
            }
        }
    }
}
