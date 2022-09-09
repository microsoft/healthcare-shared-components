// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Xunit;

namespace Microsoft.Health.Operations.UnitTests;

public class OperationStateTests
{
    public static IEnumerable<object[]> SerializationTestArguments => new object[][]
    {
        new object[] { typeof(OperationState<int>), new OperationState<int> { OperationId = Guid.NewGuid() } },
        new object[] { typeof(OperationState<int, string>), new OperationState<int, string> { OperationId = Guid.NewGuid(), Results = "Hello World" } }
    };

    [Theory]
    [MemberData(nameof(SerializationTestArguments))]
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test method.")]
    public void GivenState_WhenSerializing_OperationIdUsesProperFormat(Type type, IOperationState<int> state)
    {
        string json = JsonSerializer.Serialize(state, type);
        AssertOperationId(JsonSerializer.Deserialize<JsonElement>(json), state.OperationId.ToString(OperationId.FormatSpecifier));
    }

    private static void AssertOperationId(JsonElement element, string expected)
    {
        Assert.True(element.TryGetProperty(nameof(IOperationState<int>.OperationId), out JsonElement property));
        Assert.Equal(expected, property.GetString());
    }
}
