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

namespace Microsoft.Health.Operations.UnitTests.Serialization;

public class UtcCompatibilityJsonConverterTests
{
    private static readonly JsonSerializerOptions DefaultOptions = new();

    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("{ \"foo\": \"bar\" }")]
    [InlineData("[ 1, 2, 3 ]")]
    [InlineData("\"\"")]
    [InlineData("\"bar\"")]
    public void GivenInvalidToken_WhenReadingJson_ThenThrowJsonReaderException(string json)
    {
        Assert.Throws<JsonException>(() =>
        {
            Utf8JsonReader jsonReader = new(Encoding.UTF8.GetBytes(json));
            try
            {
                Assert.True(jsonReader.Read());
            }
            catch (JsonException)
            {
                Assert.Fail();
            }

            new UtcCompatibilityJsonConverter().Read(ref jsonReader, typeof(DateTimeOffset), DefaultOptions);
        });
    }

    [Theory]
    [InlineData("\"1234-05-06T07:08:09Z\"")]
    [InlineData("\"1234-05-06T07:08:09+00:00\"")]
    public void GivenValidToken_WhenReadingJson_ThenReturnDateTimeOffset(string json)
    {
        DateTimeOffset expected = new(1234, 5, 6, 7, 8, 9, TimeSpan.Zero);
        Utf8JsonReader jsonReader = new(Encoding.UTF8.GetBytes(json));

        Assert.True(jsonReader.Read());
        DateTimeOffset actual = new UtcCompatibilityJsonConverter().Read(ref jsonReader, typeof(DateTimeOffset), DefaultOptions);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GivenUtcDateTimeOffset_WhenWritingJson_ThenWriteZ()
    {
        DateTimeOffset dto = DateTimeOffset.UtcNow;
        using MemoryStream buffer = new();
        using Utf8JsonWriter jsonWriter = new(buffer);

        new UtcCompatibilityJsonConverter().Write(jsonWriter, dto, DefaultOptions);
        jsonWriter.Flush();

        buffer.Seek(0, SeekOrigin.Begin);
        string actual = Encoding.UTF8.GetString(buffer.ToArray());
        Assert.EndsWith("Z\"", actual, StringComparison.Ordinal);
        Assert.Equal(JsonSerializer.Serialize(dto.UtcDateTime, DefaultOptions), actual);
        Assert.NotEqual(JsonSerializer.Serialize(dto, DefaultOptions), actual);
    }

    [Fact]
    public void GivenNonUtcDateTimeOffset_WhenWritingJson_ThenWriteOffset()
    {
        DateTimeOffset dto = new(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
        using MemoryStream buffer = new();
        using Utf8JsonWriter jsonWriter = new(buffer);

        new UtcCompatibilityJsonConverter().Write(jsonWriter, dto, DefaultOptions);
        jsonWriter.Flush();

        buffer.Seek(0, SeekOrigin.Begin);
        string actual = Encoding.UTF8.GetString(buffer.ToArray());
        Assert.EndsWith("+07:00\"", actual, StringComparison.Ordinal);
        Assert.Equal(JsonSerializer.Serialize(dto, DefaultOptions), actual);
        Assert.NotEqual(JsonSerializer.Serialize(dto.DateTime, DefaultOptions), actual);
    }
}
