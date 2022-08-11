// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Control;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

public sealed class SchemaJobWorkerTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
    private readonly IMediator _mediator;
    private readonly IProcessTerminator _processTerminator;
    private readonly ILogger<SchemaJobWorker> _logger;
    private readonly ISchemaDataStore _schemaDataStore;
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
        IServiceScope scope = Substitute.For<IServiceScope>();
        var collection = new ServiceCollection();
        collection.AddSingleton<ISchemaDataStore>(_schemaDataStore);
        _serviceProvider = collection.BuildServiceProvider();
        _schemaDataStore.DeleteExpiredInstanceSchemaAsync(default).ReturnsForAnyArgs(Task.CompletedTask);

        _sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration { TerminateWhenSchemaVersionUpdatedTo = 2, SchemaOptions = new SqlServerSchemaOptions { JobPollingFrequencyInSeconds = 0 } };
        _worker = new SchemaJobWorker(_serviceProvider, Options.Create(_sqlServerDataStoreConfiguration), _mediator, _processTerminator, _logger);
    }

    [Fact]
    public async Task GivenSchemaInformation_WhenCurrentDoesNotMatchTerminateWhenSchemaVersionUpdatedTo_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2);
        info.Current = 1;
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
            await _worker.ExecuteAsync(info, "blah", _cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [Fact]
    public async Task GivenSchemaInformation_WhenCurrentMatchesTerminateWhenSchemaVersionUpdatedTo_ProcessTerminatorCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2);
        info.Current = 1;
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
            await _worker.ExecuteAsync(info, "blah", _cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.ReceivedWithAnyArgs().Terminate(default);
    }

    [Fact]
    public async Task GivenSchemaInformation_WhenCurrentIsNullAndTerminateWhenSchemaVersionUpdatedToIsNotNull_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2);
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
            await _worker.ExecuteAsync(info, "blah", _cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    [Fact]
    public async Task GivenSchemaInformation_WhenCurrentIsNullAndTerminateWhenSchemaVersionUpdatedToIsNull_ProcessTerminatorNotCalled()
    {
        SchemaInformation info = new SchemaInformation(1, 2);
        _sqlServerDataStoreConfiguration.TerminateWhenSchemaVersionUpdatedTo = null;

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
            await _worker.ExecuteAsync(info, "blah", _cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
        }

        _processTerminator.DidNotReceiveWithAnyArgs().Terminate(default);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
