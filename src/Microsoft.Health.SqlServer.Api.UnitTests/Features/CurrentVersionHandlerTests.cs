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
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Features;

public class CurrentVersionHandlerTests
{
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly IMediator _mediator;

    public CurrentVersionHandlerTests()
    {
        _schemaDataStore = Substitute.For<ISchemaDataStore>();
        var collection = new ServiceCollection();
        collection.Add(sp => new CurrentVersionHandler(_schemaDataStore)).Singleton().AsSelf().AsImplementedInterfaces();

        _mediator = new Mediator(collection.BuildServiceProvider());
    }

    [Fact]
    public async Task GivenACurrentMediator_WhenCurrentRequest_ThenReturnsCurrentVersionInformation()
    {
        string status = "completed";

        var mockCurrentVersions = new List<CurrentVersionInformation>()
        {
            new CurrentVersionInformation(1, Enum.Parse<SchemaVersionStatus>(status, true), new List<string>() { "server1", "server2" }),
            new CurrentVersionInformation(2, Enum.Parse<SchemaVersionStatus>(status, true), new List<string>()),
        };

        _schemaDataStore
            .GetCurrentVersionAsync(Arg.Any<CancellationToken>())
            .Returns(mockCurrentVersions);

        using CancellationTokenSource cts = new();
        GetCurrentVersionResponse response = await _mediator.GetCurrentVersionAsync(cts.Token);
        var currentVersionsResponse = response.CurrentVersions;

        Assert.Equal(mockCurrentVersions.Count, currentVersionsResponse.Count);
        Assert.Equal(1, currentVersionsResponse[0].Id);
        Assert.Equal(SchemaVersionStatus.completed, currentVersionsResponse[0].Status);
        Assert.Equal(2, currentVersionsResponse[0].Servers.Count);
    }

    [Fact]
    public async Task GivenACurrentMediator_WhenCurrentRequestAndEmptySchemaVersionTable_ThenReturnsEmptyArray()
    {
        var mockCurrentVersions = new List<CurrentVersionInformation>();

        _schemaDataStore
            .GetCurrentVersionAsync(Arg.Any<CancellationToken>())
            .Returns(mockCurrentVersions);

        using CancellationTokenSource cts = new();
        GetCurrentVersionResponse response = await _mediator.GetCurrentVersionAsync(cts.Token);

        Assert.Empty(response.CurrentVersions);
    }
}
