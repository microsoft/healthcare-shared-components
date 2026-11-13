// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Medino;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Control;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

[TestClass]
public sealed class SchemaJobWorkerTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
    private readonly IMediator _mediator;
    private readonly IProcessTerminator _processTerminator;
    private readonly ILogger<SchemaJobWorker> _logger;
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly ISchemaWriteGate _writeGate;
    private readonly SchemaJobWorker _worker;

    public SchemaJobWorkerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _processTerminator = Substitute.For<IProcessTerminator>();
        _logger = NullLogger<SchemaJobWorker>.Instance;
        _schemaDataStore = Substitute.For<ISchemaDataStore>();
        _schemaDataStore.DatabaseName.Returns("TestDb");
        _writeGate = Substitute.For<ISchemaWriteGate>();
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        var collection = new ServiceCollection();
        collection.AddSingleton<ISchemaDataStore>(_schemaDataStore);
        collection.AddSingleton<ISchemaWriteGate>(_writeGate);
        _serviceProvider = collection.BuildServiceProvider();
        _schemaDataStore.DeleteExpiredInstanceSchemaAsync(default).ReturnsForAnyArgs(Task.CompletedTask);

        _sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration { TerminateWhenSchemaVersionUpdatedTo = 2, SchemaOptions = new SqlServerSchemaOptions { JobPollingFrequencyInSeconds = 0 } };
        _worker = new SchemaJobWorker(_serviceProvider, Options.Create(_sqlServerDataStoreConfiguration), _mediator, _processTerminator, _logger);
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentDoesNotMatchTerminateWhenSchemaVersionUpdatedTo_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = 1 };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(1);

        await ExecuteOnceAsync(info, "blah");

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentMatchesTerminateWhenSchemaVersionUpdatedTo_ProcessTerminatorCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = 1 };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(2);

        await ExecuteOnceAsync(info, "blah");

        _processTerminator.ReceivedWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    [DataRow(0, true)]
    [DataRow(1, false)]
    public async Task GivenSchemaVersionChanged_WhenExecuting_ThenPublishesNotificationWithDatabaseName(int previousVersion, bool expectedFullSnapshot)
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = previousVersion };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(2);

        await ExecuteOnceAsync(info, "blah");

        await _mediator.Received(1).PublishAsync(
            Arg.Is<SchemaUpgradedNotification>(n => n.DatabaseName == "TestDb" && n.Version == 2 && n.IsFullSchemaSnapshot == expectedFullSnapshot),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GivenSchemaVersionUnchanged_WhenExecutingOnce_ThenNoNotificationIsPublished()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = 1 };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(1);

        await ExecuteOnceAsync(info, "blah");

        await _mediator.DidNotReceive().PublishAsync(Arg.Any<SchemaUpgradedNotification>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GivenWritableDatabase_WhenExecutingOnce_ThenExpiredInstanceSchemaIsDeleted()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = 1 };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(1);

        await ExecuteOnceAsync(info, "blah");

        await _schemaDataStore.Received(1).UpsertInstanceSchemaInformationAsync("blah", info, Arg.Any<CancellationToken>());
        await _schemaDataStore.Received(1).DeleteExpiredInstanceSchemaAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentIsNullAndTerminateWhenSchemaVersionUpdatedToIsNotNull_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = null };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(0);

        await ExecuteOnceAsync(info, "blah");

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentIsNullAndTerminateWhenSchemaVersionUpdatedToIsNull_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2);
        _sqlServerDataStoreConfiguration.TerminateWhenSchemaVersionUpdatedTo = null;

        info.Current = null;
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(0);

        await ExecuteOnceAsync(info, "blah");

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenUpsertThrows3906_SchemaVersionIsReadFromCurrentVersions()
    {
        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x => throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase));

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        await ExecuteOnceAsync(info, "secondary-pod");

        Assert.AreEqual(3, info.Current);
        await _schemaDataStore.DidNotReceiveWithAnyArgs().DeleteExpiredInstanceSchemaAsync(default);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenUpsertThrows3906_DeleteExpiredInstanceSchemaIsSkipped()
    {
        SchemaInformation info = new SchemaInformation(1, 5) { Current = 3 };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x => throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase));

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        await ExecuteOnceAsync(info, "secondary-pod");

        await _schemaDataStore.DidNotReceiveWithAnyArgs().DeleteExpiredInstanceSchemaAsync(default);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenUpsertThrows3906_OnlyCompletedVersionsAreConsidered()
    {
        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x => throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase));

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
                new CurrentVersionInformation(4, SchemaVersionStatus.started, new List<string> { "primary-pod" }),
            }));

        await ExecuteOnceAsync(info, "secondary-pod");

        // Should pick version 3 (completed), not version 4 (started)
        Assert.AreEqual(3, info.Current);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenCompletedVersionExceedsMaxSupported_CurrentIsCappedToMaxSupported()
    {
        // Pod supports up to version 5, but SQL has version 7 completed (replicated from a newer primary)
        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x => throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase));

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
                new CurrentVersionInformation(5, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
                new CurrentVersionInformation(7, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        await ExecuteOnceAsync(info, "secondary-pod");

        // Should pick version 5 (max supported), not version 7
        Assert.AreEqual(5, info.Current);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalse_WhenExecuting_UpsertIsSkipped()
    {
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(false));

        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        await ExecuteOnceAsync(info, "secondary-pod");

        await _schemaDataStore.DidNotReceive().UpsertInstanceSchemaInformationAsync(Arg.Any<string>(), Arg.Any<SchemaInformation>(), Arg.Any<CancellationToken>());
        Assert.AreEqual(3, info.Current);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsTrue_WhenExecuting_UpsertIsPerformed()
    {
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));

        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(3);

        await ExecuteOnceAsync(info, "secondary-pod");

        await _schemaDataStore.Received(1).UpsertInstanceSchemaInformationAsync(Arg.Any<string>(), Arg.Any<SchemaInformation>(), Arg.Any<CancellationToken>());
        Assert.AreEqual(3, info.Current);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsTrue_WhenUpsertThrows3906_ReadOnlyFallbackIsUsed()
    {
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));

        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x => throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase));

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        await ExecuteOnceAsync(info, "secondary-pod");

        Assert.AreEqual(3, info.Current);
    }

    [TestMethod]
    public async Task GivenUnhandledException_WhenExecutingOnce_ThenExceptionIsNotPropagated()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = 1 };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x => throw new InvalidOperationException());

        await ExecuteOnceAsync(info, "blah");

        Assert.AreEqual(1, info.Current);
        await _schemaDataStore.DidNotReceiveWithAnyArgs().DeleteExpiredInstanceSchemaAsync(default);
        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenNullScope_WhenExecutingOnce_ThenThrowsArgumentNullException()
        => await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _worker.ExecuteOnceAsync(null, new SchemaInformation(1, 2), "blah"));

    [TestMethod]
    public async Task GivenNullSchemaInformation_WhenExecutingOnce_ThenThrowsArgumentNullException()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _worker.ExecuteOnceAsync(scope, null, "blah"));
    }

    [TestMethod]
    public async Task GivenNullSchemaInformation_WhenExecuting_ThenThrowsArgumentNullException()
        => await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _worker.ExecuteAsync(null, "blah", CancellationToken.None));

    [TestMethod]
    public async Task GivenPollingLoop_WhenExecuting_ThenEachIterationUsesNewScopeUntilCancelled()
    {
        int scopedResolutions = 0;
        var collection = new ServiceCollection();
        collection.AddScoped<ISchemaDataStore>(_ =>
        {
            scopedResolutions++;
            return _schemaDataStore;
        });
        collection.AddSingleton<ISchemaWriteGate>(_writeGate);
        using ServiceProvider serviceProvider = collection.BuildServiceProvider();

        var worker = new SchemaJobWorker(serviceProvider, Options.Create(_sqlServerDataStoreConfiguration), _mediator, _processTerminator, _logger);

        // Safety net so a regression in the loop fails the test instead of hanging it.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        int upserts = 0;
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(x =>
        {
            if (++upserts == 3)
            {
                cts.Cancel();
            }

            return 1;
        });

        await worker.ExecuteAsync(new SchemaInformation(1, 2) { Current = 1 }, "blah", cts.Token);

        Assert.AreEqual(3, upserts);
        Assert.AreEqual(3, scopedResolutions);
    }

    [TestMethod]
    public async Task GivenCancelledToken_WhenExecuting_ThenReturnsWithoutPolling()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await _worker.ExecuteAsync(new SchemaInformation(1, 2), "blah", cts.Token);

        await _writeGate.DidNotReceiveWithAnyArgs().CanWriteAsync(default);
    }

    private async Task ExecuteOnceAsync(SchemaInformation schemaInformation, string instanceName)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        await _worker.ExecuteOnceAsync(scope, schemaInformation, instanceName);
    }
}
