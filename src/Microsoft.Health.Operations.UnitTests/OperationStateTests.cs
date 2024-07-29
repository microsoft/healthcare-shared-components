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
        IOperationState<int> after = new LatestOperationState()
        {
            CreatedTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            LastUpdatedTime = DateTimeOffset.UtcNow,
            OperationId = Guid.NewGuid(),
            PercentComplete = 50,
            Resources = [new Uri("https://example.com")],
            Results = "Hello World",
            Status = OperationStatus.Running,
            Type = 42,
        };

        LegacyOperationState before = new()
        {
            CreatedTime = after.CreatedTime.UtcDateTime,
            LastUpdatedTime = after.LastUpdatedTime.UtcDateTime,
            OperationId = after.OperationId,
            PercentComplete = after.PercentComplete,
            Resources = after.Resources,
            Results = after.Results,
            Status = after.Status,
            Type = after.Type,
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
