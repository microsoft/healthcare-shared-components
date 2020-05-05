// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
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

        public SchemaControllerTests()
        {
            _schemaInformation = new SchemaInformation((int)TestSchemaVersion.Version1, (int)TestSchemaVersion.Version3);

            var urlHelperFactory = Substitute.For<IUrlHelperFactory>();
            var urlHelper = Substitute.For<IUrlHelper>();
            urlHelper.RouteUrl(Arg.Any<UrlRouteContext>()).Returns("https://localhost/script");
            urlHelperFactory.GetUrlHelper(Arg.Any<ActionContext>()).Returns(urlHelper);

            var scriptProvider = Substitute.For<IScriptProvider>();

            _schemaController = new SchemaController(_schemaInformation, scriptProvider, urlHelperFactory, NullLogger<SchemaController>.Instance);
        }

        [Fact]
        public async Task GivenAScriptRequest_WhenSchemaIdFoundAndCurrentVersionIsNull_ThenReturnsFullSchemaSnapshotScriptAsync()
        {
            _schemaInformation.Current = null;
            FileContentResult result = await _schemaController.SqlScriptAsync(1, default);
            string script = result.FileContents.ToString();
            Assert.NotNull(script);
            Assert.Equal("1.sql", result.FileDownloadName);
        }

        [Fact]
        public async Task GivenAScriptRequest_WhenSchemaIdFoundAndCurrentVersionIsNotNull_ThenReturnsDiffSchemaScriptAsync()
        {
            _schemaInformation.Current = 1;
            FileContentResult result = await _schemaController.SqlScriptAsync(2, default);
            string script = result.FileContents.ToString();
            Assert.NotNull(script);
            Assert.Equal("2.diff.sql", result.FileDownloadName);
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

        [Fact]
        public void GivenACurrentVersionRequest_WhenNotImplemented_ThenNotImplementedShouldBeThrown()
        {
            Assert.Throws<NotImplementedException>(() => _schemaController.CurrentVersion());
        }

        [Fact]
        public void GivenACompatibilityRequest_WhenNotImplemented_ThenNotImplementedShouldBeThrown()
        {
            Assert.Throws<NotImplementedException>(() => _schemaController.Compatibility());
        }
    }
}
