// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using NSubstitute;
using Xunit;

namespace SchemaManager.UnitTests;

public class SqlSchemaManagerTests
{
    private readonly SqlSchemaManager _sqlSchemaManager;
    private readonly ISchemaManagerDataStore _schemaManagerDataStore = Substitute.For<ISchemaManagerDataStore>();
    private readonly ISchemaClient _client = Substitute.For<ISchemaClient>();
    private readonly IBaseSchemaRunner _baseSchemaRunner = Substitute.For<IBaseSchemaRunner>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public SqlSchemaManagerTests()
    {
        _baseSchemaRunner.EnsureBaseSchemaExistsAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
        _sqlSchemaManager = new SqlSchemaManager(_baseSchemaRunner, _schemaManagerDataStore, _client, _mediator, NullLogger<SqlSchemaManager>.Instance);
    }

    [Fact]
    public async Task GetCurrentSchema_OneSchema_Succeeds()
    {
        _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { new CurrentVersion(1, "Complete", new List<string> { "server1" }) });

        IList<CurrentVersion> current = await _sqlSchemaManager.GetCurrentSchema().ConfigureAwait(false);

        Assert.NotNull(current);
        Assert.Single(current);
        Assert.Equal(1, current[0].Id);
        await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureBaseSchemaExistsAsync(default).ConfigureAwait(false);
        await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureInstanceSchemaRecordExistsAsync(default).ConfigureAwait(false);
    }

    [Fact]
    public async Task GetCurrentSchema_EmptyList_Succeeds()
    {
        _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { });

        IList<CurrentVersion> current = await _sqlSchemaManager.GetCurrentSchema().ConfigureAwait(false);

        Assert.NotNull(current);
        Assert.Empty(current);
        await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureBaseSchemaExistsAsync(default).ConfigureAwait(false);
        await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureInstanceSchemaRecordExistsAsync(default).ConfigureAwait(false);
    }

    [Fact]
    public async Task GetAvailableSchema_SingleList_Succeeds()
    {
        _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql") });
        IList<AvailableVersion> available = await _sqlSchemaManager.GetAvailableSchema().ConfigureAwait(false);

        Assert.NotNull(available);
        Assert.Single(available);
        Assert.Equal(1, available[0].Id);
        Assert.Equal("_script/1.sql", available[0].ScriptUri);
        Assert.Equal("_script/1.diff.sql", available[0].DiffUri);
    }

    [Fact]
    public async Task GetAvailableSchema_ContainsVersionZero_RemovesZero()
    {
        _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(0, "_script/0.sql", "_script/0.diff.sql"), new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql") });
        IList<AvailableVersion> available = await _sqlSchemaManager.GetAvailableSchema().ConfigureAwait(false);

        Assert.NotNull(available);
        Assert.Single(available);
        Assert.Equal(1, available[0].Id);
        Assert.Equal("_script/1.sql", available[0].ScriptUri);
        Assert.Equal("_script/1.diff.sql", available[0].DiffUri);
    }

    [Fact]
    public async Task ApplySchema_UsingDiffScript_Succeeds()
    {
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).ReturnsForAnyArgs(Task.FromResult(1));
        _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { });
        _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql"), new AvailableVersion(2, "_script/2.sql", "_script/2.diff.sql") });
        _client.GetCompatibilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new CompatibleVersion(1, 2));
        _client.GetDiffScriptAsync(2, Arg.Any<CancellationToken>()).Returns("script");
        await _sqlSchemaManager.ApplySchema(new MutuallyExclusiveType { Latest = false, Version = 2, Next = false }).ConfigureAwait(false);
        await _schemaManagerDataStore.Received(1).ExecuteScriptAndCompleteSchemaVersionAsync(Arg.Is("script"), Arg.Is(2), Arg.Is(false), Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Fact]
    public async Task ApplySchema_UsingSnapshotScript_Succeeds()
    {
        var list1 = new List<AvailableVersion> { new AvailableVersion(0, "_script/0.sql", "_script/0.diff.sql"), new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql"), new AvailableVersion(2, "_script/2.sql", "_script/2.diff.sql") };
        var list2 = new List<AvailableVersion> { new AvailableVersion(0, "_script/0.sql", "_script/0.diff.sql"), new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql"), new AvailableVersion(2, "_script/2.sql", "_script/2.diff.sql") };
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).ReturnsForAnyArgs(Task.FromResult(0));
        _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { });
        _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(list1, list2);
        _client.GetScriptAsync(2, Arg.Any<CancellationToken>()).Returns("script");
        _client.GetCompatibilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new CompatibleVersion(1, 2));

        await _sqlSchemaManager.ApplySchema(new MutuallyExclusiveType { Version = 2 }).ConfigureAwait(false);

        await _schemaManagerDataStore.Received(1).ExecuteScriptAndCompleteSchemaVersionAsync(Arg.Is("script"), Arg.Is(2), Arg.Is(true), Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    [Fact]
    public async Task ApplySchema_OnDependencyThrowSchemaManagerException_ThrowsSchemaManagerException()
    {
        // Set a zero retry sleep duration to expedite fail-case unit test.
        _sqlSchemaManager.RetrySleepDuration = TimeSpan.Zero;

        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).ReturnsForAnyArgs(Task.FromResult(1));
        _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Task.FromException<List<CurrentVersion>>(new SchemaManagerException("anymessage")));
        _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql"), new AvailableVersion(2, "_script/2.sql", "_script/2.diff.sql") });
        _client.GetCompatibilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new CompatibleVersion(1, 2));
        _client.GetDiffScriptAsync(2, Arg.Any<CancellationToken>()).Returns("script");
        await Assert.ThrowsAsync<SchemaManagerException>(() => _sqlSchemaManager.ApplySchema(new MutuallyExclusiveType { Latest = false, Version = 2, Next = false })).ConfigureAwait(false);
    }

    [Fact]
    public async Task ApplySchema_OnDependencyThrowInvalidOperationException_ThrowsInvalidOperationException()
    {
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).ReturnsForAnyArgs(Task.FromResult(1));
        _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Task.FromException<List<CurrentVersion>>(new InvalidOperationException()));
        _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql"), new AvailableVersion(2, "_script/2.sql", "_script/2.diff.sql") });
        _client.GetCompatibilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new CompatibleVersion(1, 2));
        _client.GetDiffScriptAsync(2, Arg.Any<CancellationToken>()).Returns("script");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sqlSchemaManager.ApplySchema(new MutuallyExclusiveType { Latest = false, Version = 2, Next = false })).ConfigureAwait(false);
    }

    [Fact]
    public async Task ApplySchema_TargetVersionIsLessThanOrEqualsToTheTheCurrentSchemaVersion_ShouldNotThrowException()
    {
        _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).ReturnsForAnyArgs(Task.FromResult(2));
        _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { new CurrentVersion(2, "completed", new List<string> { "2323" }) });
        _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(2, "_script/2.sql", "_script/2.diff.sql"), new AvailableVersion(3, "_script/3.sql", "_script/3.diff.sql") });
        _client.GetCompatibilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new CompatibleVersion(1, 3));
        _client.GetDiffScriptAsync(2, Arg.Any<CancellationToken>()).Returns("script");

        await _sqlSchemaManager.ApplySchema(new MutuallyExclusiveType { Latest = false, Version = 2, Next = false }).ConfigureAwait(false);

        await _schemaManagerDataStore.DidNotReceive().ExecuteScriptAndCompleteSchemaVersionAsync(Arg.Is("script"), Arg.Is(2), Arg.Is(false), Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }
}
