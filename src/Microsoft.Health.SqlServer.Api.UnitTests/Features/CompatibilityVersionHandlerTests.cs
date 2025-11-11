// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Medino;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Features;

public class CompatibilityVersionHandlerTests
{
    private readonly ISchemaDataStore _schemaMigrationDataStore;
    private readonly IMediator _mediator;

    public CompatibilityVersionHandlerTests()
    {
        IMediatorServiceProvider sp = Substitute.For<IMediatorServiceProvider>();
        _schemaMigrationDataStore = Substitute.For<ISchemaDataStore>();
        _mediator = new Mediator(sp);

        sp.GetService<CompatibilityVersionHandler>().Returns(new CompatibilityVersionHandler(_schemaMigrationDataStore));
    }

    [Fact]
    public async Task GivenAMediator_WhenCompatibleRequest_ThenReturnsCompatibleVersions()
    {
        _schemaMigrationDataStore
            .GetLatestCompatibleVersionsAsync(Arg.Any<CancellationToken>())
            .Returns(new CompatibleVersions(1, 3));

        using CancellationTokenSource cts = new();
        GetCompatibilityVersionResponse response = await _mediator.GetCompatibleVersionAsync(cts.Token);

        Assert.Equal(1, response.CompatibleVersions.Min);
        Assert.Equal(3, response.CompatibleVersions.Max);
    }
}
