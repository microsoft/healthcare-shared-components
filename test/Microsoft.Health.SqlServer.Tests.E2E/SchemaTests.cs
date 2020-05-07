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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Health.SqlServer.Web;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.SqlServer.Tests.E2E
{
    public class SchemaTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        private readonly HttpClient _client;

        public SchemaTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                new object[] { "_schema/compatibility" },
                new object[] { "_schema/versions/current" },
            };

        public static IEnumerable<object[]> SqlData =>
            new List<object[]>
            {
                new object[] { "_schema/versions/1/script" },
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
            string scriptUrl = $"/_schema/versions/{firstResult["id"]}/script";
            Assert.Equal(scriptUrl, firstResult["script"]);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task GivenGetMethod_WhenRequestingSchema_TheServerShouldReturnNotImplemented(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotImplemented);
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

        [InlineData("_schema/versions/abc/script")]
        [InlineData("_schema/versions/abc/diff")]
        [Theory]
        public async Task GivenNonIntegerVersion_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(SqlData))]
        public async Task GivenPostMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(SqlData))]
        public async Task GivenPutMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound);
        }

        [Theory]
        [MemberData(nameof(SqlData))]
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
                RequestUri = new Uri(_client.BaseAddress, "_schema/versions/1/script"),
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

        [InlineData("_schema/versions/0/script")]
        [InlineData("_schema/versions/0/diff")]
        [Theory]
        public async Task GivenSchemaIdNotFound_WhenRequestingScript_TheServerShouldReturnNotFoundException(string path)
        {
            await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound);
        }

        private async Task SendAndVerifyStatusCode(HttpMethod httpMethod, string path, HttpStatusCode httpStatusCode)
        {
            var request = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = new Uri(_client.BaseAddress, path),
            };

            // Setting the contentType explicitly because POST/PUT/PATCH throws UnsupportedMediaType
            using (var content = new StringContent(" ", Encoding.UTF8, "application/json"))
            {
                request.Content = content;
                HttpResponseMessage response = await _client.SendAsync(request);
                Assert.Equal(httpStatusCode, response.StatusCode);
            }
        }
    }
}