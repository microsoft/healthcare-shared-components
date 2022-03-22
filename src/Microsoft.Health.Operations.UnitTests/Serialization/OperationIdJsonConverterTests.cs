// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Health.Operations.Serialization;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Health.Operations.UnitTests.Serialization;

public class OperationIdJsonConverterTests
{
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions();

    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("{ \"foo\": \"bar\" }")]
    [InlineData("[ 1, 2, 3 ]")]
    [InlineData("\"\"")]
    [InlineData("\"bar\"")]
    [InlineData("\"0123456789abcdef0123456789abcde\"")]
    public void GivenInvalidToken_WhenReadingJson_ThenThrowJsonReaderException(string json)
    {
        var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        Assert.True(jsonReader.Read());
        try
        {
            // Note: the by-ref parameter prevents the test from leveraging Assert.Throws
            new OperationIdJsonConverter().Read(ref jsonReader, typeof(Guid), DefaultOptions);
            throw new ThrowsException(typeof(JsonException));
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(JsonException))
            {
                throw new ThrowsException(typeof(JsonException), e);
            }
        }
    }

    [Fact]
    public void GivenValidToken_WhenReadingJson_ThenReturnOperationId()
    {
        Guid expected = Guid.NewGuid();
        var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes("\"" + expected.ToString(OperationId.FormatSpecifier) + "\""));

        Assert.True(jsonReader.Read());
        Guid actual = new OperationIdJsonConverter().Read(ref jsonReader, typeof(Guid), DefaultOptions);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GivenGuid_WhenWritingJson_ThenWriteStringToken()
    {
        Guid expected = Guid.NewGuid();
        using var buffer = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(buffer);

        new OperationIdJsonConverter().Write(jsonWriter, expected, DefaultOptions);
        jsonWriter.Flush();

        buffer.Seek(0, SeekOrigin.Begin);
        var jsonReader = new Utf8JsonReader(buffer.ToArray());

        Assert.Equal(expected.ToString(OperationId.FormatSpecifier), JsonSerializer.Deserialize<string>(ref jsonReader, DefaultOptions));
    }
}
