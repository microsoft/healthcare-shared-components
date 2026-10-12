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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.Tests.E2E;

[TestClass]
public class SchemaTests : SqlServerWebAppTests
{
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

    [TestMethod]
    public async Task GivenAServerThatHasSchemas_WhenRequestingAvailable_JsonShouldBeReturned()
    {
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(Client.BaseAddress, "_schema/versions"),
        };

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var jArrayResponse = JArray.Parse(await response.Content.ReadAsStringAsync());

        Assert.IsNotEmpty(jArrayResponse);

        JToken firstResult = jArrayResponse.First;
        int version = (int)firstResult["id"];

        string scriptUrl = $"/_schema/versions/{version}/script";
        Assert.AreEqual(scriptUrl, firstResult["script"].Value<string>());
        if (version == 1)
        {
            Assert.AreEqual(string.Empty, firstResult["diff"].Value<string>());
        }
        else
        {
            string diffScriptUrl = $"/_schema/versions/{version}/script/diff";
            Assert.AreEqual(diffScriptUrl, firstResult["diff"].Value<string>());
        }
    }

    [TestMethod]
    [Ignore("Deployment steps to refactor to include environmentUrl")]
    public async Task WhenRequestingSchema_GivenGetMethodAndCompatibilityPathAndInstanceSchemaTableIsEmpty_TheServerShouldReturnsNotFound()
    {
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(Client.BaseAddress, "_schema/compatibility"),
        };

        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string responseBodyAsString = await response.Content.ReadAsStringAsync();

        CompatibleVersions jsonList = JsonConvert.DeserializeObject<CompatibleVersions>(responseBodyAsString);
        Assert.IsNotNull(jsonList);
    }

    [TestMethod]
    [Ignore("Deployment steps to refactor to include environmentUrl")]
    public async Task WhenRequestingSchema_GivenGetMethodAndCurrentVersionPath_TheServerShouldReturnSuccess()
    {
        HttpResponseMessage response = await SendAndVerifyStatusCode(HttpMethod.Get, "_schema/versions/current", HttpStatusCode.OK);

        string responseBodyAsString = await response.Content.ReadAsStringAsync();
        var jsonList = JsonConvert.DeserializeObject<IList<CurrentVersionInformation>>(responseBodyAsString);
        Assert.AreEqual(2, jsonList[0].Id);
        Assert.ContainsSingle(jsonList[0].Servers);
        Assert.AreEqual(Enum.Parse<SchemaVersionStatus>("completed", ignoreCase: true), jsonList[0].Status);
    }

    [TestMethod]
    [DynamicData(nameof(Data))]
    public async Task GivenPostMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DynamicData(nameof(Data))]
    public async Task GivenPutMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DynamicData(nameof(Data))]
    public async Task GivenDeleteMethod_WhenRequestingSchema_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Delete, path, HttpStatusCode.NotFound);
    }

    [DataRow("_schema/versions/abc/script")]
    [DataRow("_schema/versions/abc/script/diff")]
    [TestMethod]
    public async Task GivenNonIntegerVersion_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DynamicData(nameof(ScriptData))]
    public async Task GivenPostMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Post, path, HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DynamicData(nameof(ScriptData))]
    public async Task GivenPutMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Put, path, HttpStatusCode.NotFound);
    }

    [TestMethod]
    [DynamicData(nameof(ScriptData))]
    public async Task GivenDeleteMethod_WhenRequestingScript_TheServerShouldReturnNotFound(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Delete, path, HttpStatusCode.NotFound);
    }

    [DataRow("_schema/versions/1/script")]
    [DataRow("_schema/versions/2/script/diff")]
    [TestMethod]
    public async Task GivenSchemaIdFound_WhenRequestingScript_TheServerShouldReturnScript(string path)
    {
        using var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(Client.BaseAddress, path),
        };
        HttpResponseMessage response = await Client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string script = response.Content.ToString();

        Assert.IsNotEmpty(script);
    }

    [DataRow("_schema/versions/0/script")]
    [DataRow("_schema/versions/0/script/diff")]
    [TestMethod]
    public async Task GivenSchemaIdNotFound_WhenRequestingScript_TheServerShouldReturnNotFoundException(string path)
    {
        await SendAndVerifyStatusCode(HttpMethod.Get, path, HttpStatusCode.NotFound);
    }

    private async Task<HttpResponseMessage> SendAndVerifyStatusCode(HttpMethod httpMethod, string path, HttpStatusCode expectedStatusCode)
    {
        using var request = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = new Uri(Client.BaseAddress, path),
        };

        HttpResponseMessage response = null;

        // Setting the contentType explicitly because POST/PUT/PATCH throws UnsupportedMediaType
        using (var content = new StringContent(" ", Encoding.UTF8, "application/json"))
        {
            request.Content = content;
            response = await Client.SendAsync(request);
            Assert.AreEqual(expectedStatusCode, response.StatusCode);
        }

        return response;
    }
}
