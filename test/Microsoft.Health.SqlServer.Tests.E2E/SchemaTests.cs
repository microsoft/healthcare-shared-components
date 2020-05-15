// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Tests.E2E.Rest;
using Microsoft.Health.SqlServer.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.SqlServer.Tests.E2E
{
    public class SchemaTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
    {
        private readonly HttpClient _client;

        public SchemaTests(HttpIntegrationTestFixture<Startup> fixture)
        {
            _client = fixture.HttpClient;
        }

        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[] { "_schema/compatibility" },
                new object[] { "_schema/versions/current" },
            };

        public static IEnumerable<object[]> ScriptData =>
            new List<object[]>
            {
                new object[] { "_schema/versions/1/snapshot" },
                new object[] { "_schema/versions/1/diff" },
            };

        [Fact]
        public async Task GivenAServerThatHasSchemas_WhenRequestingAvailable_JsonShouldBeReturned()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_client.BaseAddress, "_schema/versions"),
            };

            HttpResponseMessage response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var jArrayResponse = JArray.Parse(await response.Content.ReadAsStringAsync());

            Assert.NotEmpty(jArrayResponse);

            JToken firstResult = jArrayResponse.First;
            string scriptUrl = $"/_schema/versions/{firstResult["id"]}/snapshot";
            Assert.Equal(scriptUrl, firstResult["script"]);
        }

        [Fact(Skip = "Deployment steps to refactor to include environmentUrl")]
        public async Task WhenRequestingSchema_GivenGetMethodAndCompatibilityPathAndInstanceSchemaTableIsEmpty_TheServerShouldReturnsNotFound()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_client.BaseAddress, "_schema/compatibility"),
            };

            HttpResponseMessage response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseBodyAsString = await response.Content.ReadAsStringAsync();

            CompatibleVersions jsonList = JsonConvert.DeserializeObject<CompatibleVersions>(responseBodyAsString);
            Assert.NotNull(jsonList);
        }

        [Fact(Skip = "Deployment steps to refactor to include environmentUrl")]
        public async Task WhenRequestingSchema_GivenGetMethodAndCurrentVersionPath_TheServerShouldReturnSuccess()
        {
            HttpResponseMessage response = await SendAndVerifyStatusCode(HttpMethod.Get, "_schema/versions/current", HttpStatusCode.OK);

            string responseBodyAsString = await response.Content.ReadAsStringAsync();
            var jsonList = JsonConvert.DeserializeObject<IList<CurrentVersionInformation>>(responseBodyAsString);
            Assert.Equal(2, jsonList[0].Id);
            Assert.Equal(1, jsonList[0].Servers.Count);
            Assert.Equal((SchemaVersionStatus)Enum.Parse(typeof(SchemaVersionStatus), "completed", true), jsonList[0].Status);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task GivenPostMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task GivenPutMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task GivenDeleteMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Delete, path, HttpStatusCode.NotFound);
        }

        [InlineData("_schema/versions/abc/snapshot")]
        [InlineData("_schema/versions/abc/diff")]
        [Theory]
        public async Task GivenNonIntegerVersion_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(ScriptData))]
        public async Task GivenPostMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(ScriptData))]
        public async Task GivenPutMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(ScriptData))]
        public async Task GivenDeleteMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Delete, path, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GivenSchemaIdFound_WhenRequestingScript_TheServerShouldReturnScript()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_client.BaseAddress, "_schema/versions/1/snapshot"),
            };
            HttpResponseMessage response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string script = response.Content.ToString();

            Assert.NotEmpty(script);
        }

        [Fact]
        public async Task GivenSchemaIdFound_WhenRequestingDiff_TheServerShouldReturnDiff()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_client.BaseAddress, "_schema/versions/2/diff"),
            };
            HttpResponseMessage response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string diff = response.Content.ToString();

            Assert.NotEmpty(diff);
        }

        [InlineData("_schema/versions/0/snapshot")]
        [InlineData("_schema/versions/0/diff")]
        [Theory]
        public async Task GivenSchemaIdNotFound_WhenRequestingScript_TheServerShouldReturnNotFoundException(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound);
        }

        private async Task<HttpResponseMessage> SendAndVerifyStatusCode(HttpMethod httpMethod, string path, HttpStatusCode expectedStatusCode)
        {
            var request = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(_client.BaseAddress, path),
            };

            HttpResponseMessage response = null;

            // Setting the contentType explicitly because POST/PUT/PATCH throws UnsupportedMediaType
            using (var content = new StringContent(" ", Encoding.UTF8, "application/json"))
            {
                request.Content = content;
                response = await _client.SendAsync(request);
                Assert.Equal(expectedStatusCode, response.StatusCode);
            }

            return response;
        }
    }
}