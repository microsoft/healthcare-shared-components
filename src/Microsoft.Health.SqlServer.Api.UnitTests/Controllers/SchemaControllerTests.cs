// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Medino;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.SqlServer.Api.Controllers;
using Microsoft.Health.SqlServer.Features.Schema;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Controllers;

[TestClass]
public sealed class SchemaControllerTests : IDisposable
{
    private readonly SchemaController _schemaController;
    private readonly SchemaInformation _schemaInformation;
    private readonly IMediator _mediator;

    public SchemaControllerTests()
    {
        _schemaInformation = new SchemaInformation((int)TestSchemaVersion.Version1, (int)TestSchemaVersion.Version3);
        _mediator = Substitute.For<IMediator>();

        var urlHelperFactory = Substitute.For<IUrlHelperFactory>();
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.RouteUrl(Arg.Any<UrlRouteContext>()).Returns("https://localhost/script");
        urlHelperFactory.GetUrlHelper(Arg.Any<ActionContext>()).Returns(urlHelper);

        var scriptProvider = Substitute.For<IScriptProvider>();

        _schemaController = new SchemaController(_schemaInformation, scriptProvider, urlHelperFactory, _mediator, NullLogger<SchemaController>.Instance);
    }

    [TestMethod]
    public void GivenAnAvailableVersionsRequest_WhenCurrentVersionIsNull_ThenAllVersionsReturned()
    {
        ActionResult result = _schemaController.AvailableVersions();

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);

        var jArrayResult = JArray.FromObject(jsonResult.Value);
        Assert.AreEqual(Enum.GetNames<TestSchemaVersion>().Length - 1, jArrayResult.Count);

        JToken firstResult = jArrayResult.First;
        Assert.AreEqual(1, firstResult["id"].Value<int>());
        Assert.AreEqual("https://localhost/script", firstResult["script"].Value<string>());
        Assert.AreEqual(string.Empty, firstResult["diff"].Value<string>());

        // Ensure available versions are in the ascending order
        jArrayResult.RemoveAt(0);
        var previousId = firstResult["id"].Value<int>();
        foreach (JToken item in jArrayResult)
        {
            var currentId = (int)item["id"];
            Assert.IsLessThan(currentId, previousId, "The available versions are not in the ascending order");
        }
    }

    [TestMethod]
    public void GivenAnAvailableVersionsRequest_WhenCurrentVersionNotNull_ThenCorrectVersionsReturned()
    {
        _schemaInformation.Current = (int)TestSchemaVersion.Version2;
        ActionResult result = _schemaController.AvailableVersions();

        var jsonResult = result as JsonResult;
        Assert.IsNotNull(jsonResult);

        var jArrayResult = JArray.FromObject(jsonResult.Value);
        Assert.AreEqual(Enum.GetNames<TestSchemaVersion>().Length - 2, jArrayResult.Count);

        JToken firstResult = jArrayResult.First;
        Assert.AreEqual(2, firstResult["id"].Value<int>());
        Assert.AreEqual("https://localhost/script", firstResult["script"].Value<string>());
        Assert.AreEqual("https://localhost/script", firstResult["diff"].Value<string>());
    }

    public void Dispose()
    {
        _schemaController.Dispose();
        GC.SuppressFinalize(this);
    }
}
