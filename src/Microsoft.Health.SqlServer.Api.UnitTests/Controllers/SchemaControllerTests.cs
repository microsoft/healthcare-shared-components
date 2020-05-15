// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.SqlServer.Api.Controllers;
using Microsoft.Health.SqlServer.Features.Schema;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Controllers
{
    public class SchemaControllerTests
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

        [Fact]
        public void GivenAnAvailableVersionsRequest_WhenCurrentVersionIsNull_ThenAllVersionsReturned()
        {
            ActionResult result = _schemaController.AvailableVersions();

            var jsonResult = result as JsonResult;
            Assert.NotNull(jsonResult);

            var jArrayResult = JArray.FromObject(jsonResult.Value);
            Assert.Equal(Enum.GetNames(typeof(TestSchemaVersion)).Length, jArrayResult.Count);

            JToken firstResult = jArrayResult.First;
            Assert.Equal(1, firstResult["id"]);
            Assert.Equal("https://localhost/script", firstResult["script"]);

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
            Assert.Equal(Enum.GetNames(typeof(TestSchemaVersion)).Length - 1, jArrayResult.Count);

            JToken firstResult = jArrayResult.First;
            Assert.Equal(2, firstResult["id"]);
            Assert.Equal("https://localhost/script", firstResult["script"]);
        }
    }
}
