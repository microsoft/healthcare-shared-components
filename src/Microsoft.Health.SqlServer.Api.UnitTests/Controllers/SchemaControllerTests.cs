// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.SqlServer.Api.Controllers;
using Microsoft.Health.SqlServer.Features.Schema;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Controllers;

public sealed class SchemaControllerTests : IDisposable
{
    private readonly SchemaController _schemaController;
    private readonly ISchemaDataStore _schemaDataStore;
    private readonly SchemaInformation _schemaInformation;

    public SchemaControllerTests()
    {
        _schemaDataStore = Substitute.For<ISchemaDataStore>();
        _schemaInformation = new SchemaInformation((int)TestSchemaVersion.Version1, (int)TestSchemaVersion.Version3);

        var urlHelperFactory = Substitute.For<IUrlHelperFactory>();
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.RouteUrl(Arg.Any<UrlRouteContext>()).Returns("https://localhost/script");
        urlHelperFactory.GetUrlHelper(Arg.Any<ActionContext>()).Returns(urlHelper);

        var scriptProvider = Substitute.For<IScriptProvider>();

        _schemaController = new SchemaController(_schemaDataStore, _schemaInformation, scriptProvider, urlHelperFactory, NullLogger<SchemaController>.Instance);
    }

    [Fact]
    public void GivenAnAvailableVersionsRequest_WhenCurrentVersionIsNull_ThenAllVersionsReturned()
    {
        ActionResult result = _schemaController.AvailableVersions();

        var jsonResult = result as JsonResult;
        Assert.NotNull(jsonResult);

        var jArrayResult = JArray.FromObject(jsonResult.Value);
        Assert.Equal(Enum.GetNames<TestSchemaVersion>().Length - 1, jArrayResult.Count);

        JToken firstResult = jArrayResult.First;
        Assert.Equal(1, firstResult["id"]);
        Assert.Equal("https://localhost/script", firstResult["script"]);
        Assert.Equal(string.Empty, firstResult["diff"]);

        // Ensure available versions are in the ascending order
        jArrayResult.RemoveAt(0);
        var previousId = (int)firstResult["id"];
        foreach (JToken item in jArrayResult)
        {
            var currentId = (int)item["id"];
            Assert.True(previousId < currentId, "The available versions are not in the ascending order");
        }
    }

    [Fact]
    public void GivenAnAvailableVersionsRequest_WhenCurrentVersionNotNull_ThenCorrectVersionsReturned()
    {
        _schemaInformation.Current = (int)TestSchemaVersion.Version2;
        ActionResult result = _schemaController.AvailableVersions();

        var jsonResult = result as JsonResult;
        Assert.NotNull(jsonResult);

        var jArrayResult = JArray.FromObject(jsonResult.Value);
        Assert.Equal(Enum.GetNames<TestSchemaVersion>().Length - 2, jArrayResult.Count);

        JToken firstResult = jArrayResult.First;
        Assert.Equal(2, firstResult["id"]);
        Assert.Equal("https://localhost/script", firstResult["script"]);
        Assert.Equal("https://localhost/script", firstResult["diff"]);
    }

    public void Dispose()
    {
        _schemaController.Dispose();
        GC.SuppressFinalize(this);
    }
}
