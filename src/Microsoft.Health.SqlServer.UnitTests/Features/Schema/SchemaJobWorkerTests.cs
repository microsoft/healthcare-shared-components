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
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

[TestClass]
public sealed class SchemaJobWorkerTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SqlServerDataStoreOptions _options;
    private readonly IMediator _mediator;
    private readonly IProcessTerminator _processTerminator;
    private readonly ILogger<SchemaJobWorker> _logger;
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly ISchemaWriteGate _writeGate;
    private readonly SchemaJobWorker _worker;
    private int _callCount;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource(1000);

    public SchemaJobWorkerTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _mediator = Substitute.For<IMediator>();
        _processTerminator = Substitute.For<IProcessTerminator>();
        _logger = NullLogger<SchemaJobWorker>.Instance;
        _schemaDataStore = Substitute.For<ISchemaDataStore>();
        _writeGate = Substitute.For<ISchemaWriteGate>();
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        var collection = new ServiceCollection();
        collection.AddSingleton<ISchemaDataStore>(_schemaDataStore);
        collection.AddSingleton<ISchemaWriteGate>(_writeGate);
        _serviceProvider = collection.BuildServiceProvider();
        _schemaDataStore.DeleteExpiredInstanceSchemaAsync(default).ReturnsForAnyArgs(Task.CompletedTask);

        _options = new SqlServerDataStoreOptions { SchemaOptions = new SqlServerSchemaOptions { CompletionSchemaVersion = 2, JobPollingFrequency = TimeSpan.Zero } };
        _worker = new SchemaJobWorker(_serviceProvider, Options.Create(_options), _mediator, _processTerminator, _logger);
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentDoesNotMatchCompletionSchemaVersion_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = 1 };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(x =>
        {
            if (_callCount++ > 1)
            {
                _cts.Cancel();
            }

            return 1;
        });

        try
        {
            await _worker.ExecuteAsync(info, "blah", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentMatchesCompletionSchemaVersion_ProcessTerminatorCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = 1 };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(x =>
        {
            if (_callCount++ > 1)
            {
                _cts.Cancel();
            }

            return 2;
        });

        try
        {
            await _worker.ExecuteAsync(info, "blah", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.ReceivedWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentIsNullAndCompletionSchemaVersionIsNotNull_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2) { Current = null };
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(x =>
        {
            if (_callCount++ > 1)
            {
                _cts.Cancel();
            }

            return 0;
        });

        try
        {
            await _worker.ExecuteAsync(info, "blah", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenSchemaInformation_WhenCurrentIsNullAndCompletionSchemaVersionIsNull_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2);
        _options.SchemaOptions.CompletionSchemaVersion = null;

        info.Current = null;
        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(x =>
        {
            if (_callCount++ > 1)
            {
                _cts.Cancel();
            }

            return 0;
        });

        try
        {
            await _worker.ExecuteAsync(info, "blah", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenUpsertThrows3906_SchemaVersionIsReadFromCurrentVersions()
    {
        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x =>
        {
            if (_callCount++ > 0)
            {
                _cts.Cancel();
            }

            throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase);
        });

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        try
        {
            await _worker.ExecuteAsync(info, "secondary-pod", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        Assert.AreEqual(3, info.Current);
        await _schemaDataStore.DidNotReceiveWithAnyArgs().DeleteExpiredInstanceSchemaAsync(default);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenUpsertThrows3906_DeleteExpiredInstanceSchemaIsSkipped()
    {
        SchemaInformation info = new SchemaInformation(1, 5) { Current = 3 };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x =>
        {
            if (_callCount++ > 0)
            {
                _cts.Cancel();
            }

            throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase);
        });

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        try
        {
            await _worker.ExecuteAsync(info, "secondary-pod", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        await _schemaDataStore.DidNotReceiveWithAnyArgs().DeleteExpiredInstanceSchemaAsync(default);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenUpsertThrows3906_OnlyCompletedVersionsAreConsidered()
    {
        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x =>
        {
            if (_callCount++ > 0)
            {
                _cts.Cancel();
            }

            throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase);
        });

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
                new CurrentVersionInformation(4, SchemaVersionStatus.started, new List<string> { "primary-pod" }),
            }));

        try
        {
            await _worker.ExecuteAsync(info, "secondary-pod", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        // Should pick version 3 (completed), not version 4 (started)
        Assert.AreEqual(3, info.Current);
    }

    [TestMethod]
    public async Task GivenReadOnlyDatabase_WhenCompletedVersionExceedsMaxSupported_CurrentIsCappedToMaxSupported()
    {
        // Pod supports up to version 5, but SQL has version 7 completed (replicated from a newer primary)
        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x =>
        {
            if (_callCount++ > 0)
            {
                _cts.Cancel();
            }

            throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase);
        });

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
                new CurrentVersionInformation(5, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
                new CurrentVersionInformation(7, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        try
        {
            await _worker.ExecuteAsync(info, "secondary-pod", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        // Should pick version 5 (max supported), not version 7
        Assert.AreEqual(5, info.Current);
    }

    [TestMethod]
    public async Task GivenDatabaseContext_WhenExecuting_ThenLogNotSupportedAndSkipSchemaWork()
    {
        var collection = new ServiceCollection();
        collection.AddSingleton(_schemaDataStore);
        collection.AddSingleton(_writeGate);
        collection.AddScoped(_ => Substitute.For<ISqlServerDatabaseContext>());
        using ServiceProvider serviceProvider = collection.BuildServiceProvider();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        ILogger<SchemaJobWorker> logger = Substitute.For<ILogger<SchemaJobWorker>>();
        logger
            .When(x => x.Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), Arg.Any<Exception>(), Arg.Any<Func<Arg.AnyType, Exception, string>>()))
            .Do(_ => cts.Cancel());

        var worker = new SchemaJobWorker(serviceProvider, Options.Create(_options), _mediator, _processTerminator, logger);

        await worker.ExecuteAsync(new SchemaInformation(1, 2), "instance", cts.Token);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<Arg.AnyType>(),
            Arg.Is<Exception>(e => e is NotSupportedException),
            Arg.Any<Func<Arg.AnyType, Exception, string>>());
        await _writeGate.DidNotReceiveWithAnyArgs().CanWriteAsync(default);
        await _schemaDataStore.DidNotReceiveWithAnyArgs().UpsertInstanceSchemaInformationAsync(default, default, default);
        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsFalse_WhenExecuting_UpsertIsSkipped()
    {
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(false));

        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(x =>
        {
#pragma warning disable CA1849
            if (_callCount++ > 0)
            {
                _cts.Cancel();
            }
#pragma warning restore CA1849

            return Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            });
        });

        try
        {
            await _worker.ExecuteAsync(info, "secondary-pod", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        await _schemaDataStore.DidNotReceive().UpsertInstanceSchemaInformationAsync(Arg.Any<string>(), Arg.Any<SchemaInformation>(), Arg.Any<CancellationToken>());
        Assert.AreEqual(3, info.Current);
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsTrue_WhenExecuting_UpsertIsPerformed()
    {
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));

        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs(x =>
        {
#pragma warning disable CA1849
            if (_callCount++ > 0)
            {
                _cts.Cancel();
            }
#pragma warning restore CA1849

            return Task.FromResult(3);
        });

        try
        {
            await _worker.ExecuteAsync(info, "secondary-pod", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        await _schemaDataStore.Received().UpsertInstanceSchemaInformationAsync(Arg.Any<string>(), Arg.Any<SchemaInformation>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GivenWriteGateReturnsTrue_WhenUpsertThrows3906_ReadOnlyFallbackIsUsed()
    {
        _writeGate.CanWriteAsync(default).ReturnsForAnyArgs(Task.FromResult(true));

        SchemaInformation info = new SchemaInformation(1, 5) { Current = null };

        _schemaDataStore.UpsertInstanceSchemaInformationAsync(default, default, default).ReturnsForAnyArgs<int>(x =>
        {
#pragma warning disable CA1849
            if (_callCount++ > 0)
            {
                _cts.Cancel();
            }
#pragma warning restore CA1849

            throw SqlExceptionFactory.Create(SqlErrorCodes.ReadOnlyDatabase);
        });

        _schemaDataStore.GetCurrentVersionAsync(default).ReturnsForAnyArgs(
            Task.FromResult(new List<CurrentVersionInformation>
            {
                new CurrentVersionInformation(3, SchemaVersionStatus.completed, new List<string> { "primary-pod" }),
            }));

        try
        {
            await _worker.ExecuteAsync(info, "secondary-pod", _cts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        Assert.AreEqual(3, info.Current);
    }
}
