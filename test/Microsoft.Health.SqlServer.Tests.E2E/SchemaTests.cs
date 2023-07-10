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
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.Health.SqlServer.Tests.E2E.Rest;
using Microsoft.Health.SqlServer.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.SqlServer.Tests.E2E;

public class SchemaTests : IClassFixture<HttpIntegrationTestFixture<Startup>>
{
    private readonly HttpClient _client;

    public SchemaTests(HttpIntegrationTestFixture<Startup> fixture)
    {
        _client = EnsureArg.IsNotNull(fixture, nameof(fixture)).HttpClient;
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
            new object[] { "_schema/versions/1/script" },
            new object[] { "_schema/versions/2/script/diff" },
        };

    [Fact]
    public async Task GivenAServerThatHasSchemas_WhenRequestingAvailable_JsonShouldBeReturned()
    {
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(_client.BaseAddress, "_schema/versions"),
        };

        HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jArrayResponse = JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

        Assert.NotEmpty(jArrayResponse);

        JToken firstResult = jArrayResponse.First;
        int version = (int)firstResult["id"];

        string scriptUrl = $"/_schema/versions/{version}/script";
        Assert.Equal(scriptUrl, firstResult["script"]);
        if (version == 1)
        {
            Assert.Equal(string.Empty, firstResult["diff"]);
        }
        else
        {
            string diffScriptUrl = $"/_schema/versions/{version}/script/diff";
            Assert.Equal(diffScriptUrl, firstResult["diff"]);
        }
    }

    [Fact(Skip = "Deployment steps to refactor to include environmentUrl")]
    public async Task WhenRequestingSchema_GivenGetMethodAndCompatibilityPathAndInstanceSchemaTableIsEmpty_TheServerShouldReturnsNotFound()
    {
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(_client.BaseAddress, "_schema/compatibility"),
        };

        HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string responseBodyAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        CompatibleVersions jsonList = JsonConvert.DeserializeObject<CompatibleVersions>(responseBodyAsString);
        Assert.NotNull(jsonList);
    }

    [Fact(Skip = "Deployment steps to refactor to include environmentUrl")]
    public async Task WhenRequestingSchema_GivenGetMethodAndCurrentVersionPath_TheServerShouldReturnSuccess()
    {
        HttpResponseMessage response = await SendAndVerifyStatusCode(HttpMethod.Get, "_schema/versions/current", HttpStatusCode.OK).ConfigureAwait(false);

        string responseBodyAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var jsonList = JsonConvert.DeserializeObject<IList<CurrentVersionInformation>>(responseBodyAsString);
        Assert.Equal(2, jsonList[0].Id);
        Assert.Single(jsonList[0].Servers);
        Assert.Equal((SchemaVersionStatus)Enum.Parse(typeof(SchemaVersionStatus), "completed", true), jsonList[0].Status);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task GivenPostMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task GivenPutMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task GivenDeleteMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Delete, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    [InlineData("_schema/versions/abc/script")]
    [InlineData("_schema/versions/abc/script/diff")]
    [Theory]
    public async Task GivenNonIntegerVersion_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    [Theory]
    [MemberData(nameof(ScriptData))]
    public async Task GivenPostMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    [Theory]
    [MemberData(nameof(ScriptData))]
    public async Task GivenPutMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    [Theory]
    [MemberData(nameof(ScriptData))]
    public async Task GivenDeleteMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Delete, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    [InlineData("_schema/versions/1/script")]
    [InlineData("_schema/versions/2/script/diff")]
    [Theory]
    public async Task GivenSchemaIdFound_WhenRequestingScript_TheServerShouldReturnScript(string path)
    {
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(_client.BaseAddress, path),
        };
        HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string script = response.Content.ToString();

        Assert.NotEmpty(script);
    }

    [InlineData("_schema/versions/0/script")]
    [InlineData("_schema/versions/0/script/diff")]
    [Theory]
    public async Task GivenSchemaIdNotFound_WhenRequestingScript_TheServerShouldReturnNotFoundException(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAndVerifyStatusCode(HttpMethod httpMethod, string path, HttpStatusCode expectedStatusCode)
    {
        using var request = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = new Uri(_client.BaseAddress, path),
        };

        HttpResponseMessage response = null;

        // Setting the contentType explicitly because POST/PUT/PATCH throws UnsupportedMediaType
        using (var content = new StringContent(" ", Encoding.UTF8, "application/json"))
        {
            request.Content = content;
            response = await _client.SendAsync(request).ConfigureAwait(false);
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        return response;
    }
}
