// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Health.Operations.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Operations.UnitTests.Serialization;

[TestClass]
public class OperationIdJsonConverterTests
{
    private static readonly JsonSerializerOptions DefaultOptions = new();

    [TestMethod]
    [DataRow("null")]
    [DataRow("42")]
    [DataRow("{ \"foo\": \"bar\" }")]
    [DataRow("[ 1, 2, 3 ]")]
    [DataRow("\"\"")]
    [DataRow("\"bar\"")]
    [DataRow("\"0123456789abcdef0123456789abcde\"")]
    public void GivenInvalidToken_WhenReadingJson_ThenThrowJsonReaderException(string json)
    {
        Assert.Throws<JsonException>(() =>
        {
            Utf8JsonReader jsonReader = new(Encoding.UTF8.GetBytes(json));
            try
            {
                Assert.IsTrue(jsonReader.Read());
            }
            catch (JsonException)
            {
                Assert.Fail();
            }

            new OperationIdJsonConverter().Read(ref jsonReader, typeof(Guid), DefaultOptions);
        });
    }

    [TestMethod]
    public void GivenValidToken_WhenReadingJson_ThenReturnOperationId()
    {
        Guid expected = Guid.NewGuid();
        Utf8JsonReader jsonReader = new(Encoding.UTF8.GetBytes("\"" + expected.ToString(OperationId.FormatSpecifier) + "\""));

        Assert.IsTrue(jsonReader.Read());
        Guid actual = new OperationIdJsonConverter().Read(ref jsonReader, typeof(Guid), DefaultOptions);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void GivenGuid_WhenWritingJson_ThenWriteStringToken()
    {
        Guid expected = Guid.NewGuid();
        using MemoryStream buffer = new();
        using Utf8JsonWriter jsonWriter = new(buffer);

        new OperationIdJsonConverter().Write(jsonWriter, expected, DefaultOptions);
        jsonWriter.Flush();

        buffer.Seek(0, SeekOrigin.Begin);
        Utf8JsonReader jsonReader = new(buffer.ToArray());

        Assert.AreEqual(expected.ToString(OperationId.FormatSpecifier), JsonSerializer.Deserialize<string>(ref jsonReader, DefaultOptions));
    }
}
