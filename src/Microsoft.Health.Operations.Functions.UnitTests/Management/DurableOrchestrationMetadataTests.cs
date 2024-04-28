// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Operations.Functions.DurableTask;
using Microsoft.Health.Operations.Functions.Management;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.Management;

[Obsolete($"Delete after {nameof(OrchestrationInstanceMetadata)} is the only option.")]
public class DurableOrchestrationMetadataTests
{
    private static readonly JToken NullToken = JToken.Parse("null");
    private static readonly JsonSerializerSettings InProcessJsonSettings = new DurableTaskSerializerSettingsFactory().CreateJsonSerializerSettings();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GivenExistingData_WhenRoundTrippingSerialization_ThenReadSameDataWithoutHistory(bool includeTokens)
    {
        DurableOrchestrationStatus original = new()
        {
            CreatedTime = DateTime.UtcNow.AddHours(-1),
            CustomStatus = includeTokens ? JToken.Parse("{ \"Value\": \"CustomStatus\" }") : null,
            History = includeTokens ? JArray.Parse("[{ \"Value\": \"History\" }]") : null,
            Input = includeTokens ? JToken.Parse("{ \"Value\": \"Input\" }") : null,
            InstanceId = Guid.NewGuid().ToString(),
            LastUpdatedTime = DateTime.UtcNow,
            Name = nameof(GivenExistingData_WhenRoundTrippingSerialization_ThenReadSameDataWithoutHistory),
            Output = includeTokens ? JToken.Parse("{ \"Value\": \"Output\" }") : null,
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };

        // Original JSON -> Transitional JSON
        string originalJson = JsonConvert.SerializeObject(original, InProcessJsonSettings);
        DurableOrchestrationMetadata? transitional = JsonConvert.DeserializeObject<DurableOrchestrationMetadata?>(originalJson, InProcessJsonSettings);
        Assert.NotNull(transitional);
        AssertBackwardsCompatible(original, transitional);

        // Transitional JSON -> Latest JSON
        string transitionalJson = JsonConvert.SerializeObject(transitional, InProcessJsonSettings);
        OrchestrationInstanceMetadata? latest = System.Text.Json.JsonSerializer.Deserialize<OrchestrationInstanceMetadata?>(transitionalJson);
        Assert.NotNull(latest);
        AssertBackwardsCompatible(transitional, latest);

        // Latest JSON -> Transitional JSON
        string latestJson = System.Text.Json.JsonSerializer.Serialize(latest);
        transitional = JsonConvert.DeserializeObject<DurableOrchestrationMetadata?>(latestJson, InProcessJsonSettings);
        Assert.NotNull(transitional);
        AssertForwardsCompatible(latest, transitional);

        // Transitional JSON -> Original JSON
        transitionalJson = JsonConvert.SerializeObject(transitional, InProcessJsonSettings);
        DurableOrchestrationStatus? roundTrip = JsonConvert.DeserializeObject<DurableOrchestrationStatus?>(transitionalJson, InProcessJsonSettings);
        Assert.NotNull(roundTrip);
        AssertForwardsCompatible(transitional, roundTrip);

        // Compare the round-tripped data with the original data
        Assert.Equal(original.CreatedTime, roundTrip.CreatedTime);
        Assert.Equal(original.CustomStatus ?? NullToken, roundTrip.CustomStatus);
        Assert.Null(roundTrip.History); // Does not exist in the latest data model
        Assert.Equal(original.Input ?? NullToken, roundTrip.Input);
        Assert.Equal(original.InstanceId, roundTrip.InstanceId);
        Assert.Equal(original.LastUpdatedTime, roundTrip.LastUpdatedTime);
        Assert.Equal(original.Name, roundTrip.Name);
        Assert.Equal(original.Output ?? NullToken, roundTrip.Output);
        Assert.Equal(original.RuntimeStatus, roundTrip.RuntimeStatus);
    }

    private static void AssertBackwardsCompatible(DurableOrchestrationStatus expected, DurableOrchestrationMetadata actual)
    {
        Assert.Equal(expected.CreatedTime, actual.CreatedTime);
        Assert.Equal(expected.CustomStatus ?? NullToken, actual.CustomStatus);
        Assert.Equal(expected.History, actual.History);
        Assert.Equal(expected.Input ?? NullToken, actual.Input);
        Assert.Equal(expected.InstanceId, actual.InstanceId);
        Assert.Equal(expected.LastUpdatedTime, actual.LastUpdatedTime);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Output ?? NullToken, actual.Output);
        Assert.Equal(expected.RuntimeStatus, actual.RuntimeStatus);
    }

    private static void AssertBackwardsCompatible(DurableOrchestrationMetadata expected, OrchestrationInstanceMetadata actual)
    {
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.InstanceId, actual.InstanceId);
        Assert.Equal(expected.LastUpdatedAt, actual.LastUpdatedAt);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SerializedCustomStatus, actual.SerializedCustomStatus);
        Assert.Equal(expected.SerializedInput, actual.SerializedInput);
        Assert.Equal(expected.SerializedOutput, actual.SerializedOutput);
        Assert.Equal(expected.RuntimeStatus, actual.RuntimeStatus);
    }

    private static void AssertForwardsCompatible(OrchestrationInstanceMetadata expected, DurableOrchestrationMetadata actual)
    {
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.InstanceId, actual.InstanceId);
        Assert.Equal(expected.LastUpdatedAt, actual.LastUpdatedAt);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SerializedCustomStatus, actual.SerializedCustomStatus);
        Assert.Equal(expected.SerializedInput, actual.SerializedInput);
        Assert.Equal(expected.SerializedOutput, actual.SerializedOutput);
        Assert.Equal(expected.RuntimeStatus, actual.RuntimeStatus);
    }

    private static void AssertForwardsCompatible(DurableOrchestrationMetadata expected, DurableOrchestrationStatus actual)
    {
        Assert.Equal(expected.CreatedTime, actual.CreatedTime);
        Assert.Equal(expected.CustomStatus, actual.CustomStatus);
        Assert.Null(actual.History); // Does not exist in the latest data model
        Assert.Equal(expected.Input, actual.Input);
        Assert.Equal(expected.InstanceId, actual.InstanceId);
        Assert.Equal(expected.LastUpdatedTime, actual.LastUpdatedTime);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Output, actual.Output);
        Assert.Equal(expected.RuntimeStatus, actual.RuntimeStatus);
    }
}
