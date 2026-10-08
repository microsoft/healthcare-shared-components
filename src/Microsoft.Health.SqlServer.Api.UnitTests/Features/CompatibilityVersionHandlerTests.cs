// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Medino;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Features;

[TestClass]
public class CompatibilityVersionHandlerTests
{
    private readonly ISchemaDataStore _schemaMigrationDataStore;
    private readonly IMediator _mediator;

    public CompatibilityVersionHandlerTests()
    {
        _schemaMigrationDataStore = Substitute.For<ISchemaDataStore>();
        var collection = new ServiceCollection();
        collection.Add(_ => new CompatibilityVersionHandler(_schemaMigrationDataStore)).Singleton().AsSelf().AsImplementedInterfaces();

        _mediator = new Mediator(collection.BuildServiceProvider());
    }

    [TestMethod]
    public async Task GivenAMediator_WhenCompatibleRequest_ThenReturnsCompatibleVersions()
    {
        _schemaMigrationDataStore
            .GetLatestCompatibleVersionsAsync(Arg.Any<CancellationToken>())
            .Returns(new CompatibleVersions(1, 3));

        using CancellationTokenSource cts = new();
        GetCompatibilityVersionResponse response = await _mediator.GetCompatibleVersionAsync(cts.Token);

        Assert.AreEqual(1, response.CompatibleVersions.Min);
        Assert.AreEqual(3, response.CompatibleVersions.Max);
    }
}
