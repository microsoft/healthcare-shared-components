// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Health.Operations.Serialization;
using Xunit;

namespace Microsoft.Health.Operations.UnitTests;

public class OperationStateTests
{
    public static IEnumerable<object[]> SerializationTestArguments =>
    [
        [typeof(OperationState<int>), new OperationState<int> { OperationId = Guid.NewGuid() }],
        [typeof(OperationState<int, string>), new OperationState<int, string> { OperationId = Guid.NewGuid(), Results = "Hello World" }]
    ];

    [Theory]
    [MemberData(nameof(SerializationTestArguments))]
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test method.")]
    public void GivenState_WhenSerializing_OperationIdUsesProperFormat(Type type, IOperationState<int> state)
    {
        string json = JsonSerializer.Serialize(state, type);
        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);

        Assert.True(element.TryGetProperty(nameof(IOperationState<int>.OperationId), out JsonElement property));
        Assert.Equal(state.OperationId.ToString(OperationId.FormatSpecifier), property.GetString());
    }

    [Fact]
    public void GivenDateTimeRepresentations_WhenSerializing_ThenBothAreEquivalent()
    {
        LegacyOperationState before = new()
        {
            CreatedTime = DateTime.UtcNow.AddMinutes(-5),
            LastUpdatedTime = DateTime.UtcNow,
            OperationId = Guid.NewGuid(),
            PercentComplete = 50,
            Resources = [new Uri("https://example.com")],
            Results = "Hello World",
            Status = OperationStatus.Running,
            Type = 42,
        };

        IOperationState<int> after = new LatestOperationState()
        {
            CreatedTime = before.CreatedTime,
            LastUpdatedTime = before.LastUpdatedTime,
            OperationId = before.OperationId,
            PercentComplete = before.PercentComplete,
            Resources = before.Resources,
            Results = before.Results,
            Status = before.Status,
            Type = before.Type,
        };

        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));
    }

    private sealed class LegacyOperationState
    {
        [JsonConverter(typeof(OperationIdJsonConverter))]
        public Guid OperationId { get; set; }

        public int Type { get; set; }

        public DateTime CreatedTime { get; set; }

        public DateTime LastUpdatedTime { get; set; }

        public OperationStatus Status { get; set; }

        public int? PercentComplete { get; set; }

        public IReadOnlyCollection<Uri>? Resources { get; set; }

        public object? Results { get; set; }
    }

    private sealed class LatestOperationState : IOperationState<int>
    {
        public Guid OperationId { get; set; }

        public int Type { get; set; }

        public DateTimeOffset CreatedTime { get; set; }

        public DateTimeOffset LastUpdatedTime { get; set; }

        public OperationStatus Status { get; set; }

        public int? PercentComplete { get; set; }

        public IReadOnlyCollection<Uri>? Resources { get; set; }

        public object? Results { get; set; }
    }
}
