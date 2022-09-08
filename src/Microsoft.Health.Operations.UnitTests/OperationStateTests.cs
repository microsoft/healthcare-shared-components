// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Health.Operations.UnitTests;

public class OperationStateTests
{
    [Fact]
    public void GivenState_WhenSerializing_OperationIdUsesProperFormat()
    {
        string json;

        // No payload
        var s1 = new OperationState<int> { OperationId = Guid.NewGuid() };
        json = JsonSerializer.Serialize(s1);
        AssertOperationId(JsonSerializer.Deserialize<JsonElement>(json), s1.OperationId.ToString(OperationId.FormatSpecifier));

        // With payload
        var s2 = new OperationState<int, string> { OperationId = Guid.NewGuid(), Results = "Hello World" };
        json = JsonSerializer.Serialize(s2);
        AssertOperationId(JsonSerializer.Deserialize<JsonElement>(json), s2.OperationId.ToString(OperationId.FormatSpecifier));
    }

    private static void AssertOperationId(JsonElement element, string expected)
    {
        Assert.True(element.TryGetProperty(nameof(IOperationState<int>.OperationId), out JsonElement property));
        Assert.Equal(expected, property.GetString());
    }
}
