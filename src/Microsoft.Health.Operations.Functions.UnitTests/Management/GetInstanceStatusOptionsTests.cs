// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Operations.Functions.DurableTask;
using Microsoft.Health.Operations.Functions.Management;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.Management;

[Obsolete($"Delete after {nameof(GetInstanceOptions)} is the only option.")]
public class GetInstanceStatusOptionsTests
{
    private static readonly JsonSerializerSettings InProcessJsonSettings = new DurableTaskSerializerSettingsFactory().CreateJsonSerializerSettings();

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void GivenExistingData_WhenRoundTrippingSerialization_ThenReadSameDataWithoutShowHistoryOutput(bool showHistory, bool showHistoryOutput, bool showInput)
    {
        GetInstanceStatusOptions original = new()
        {
            ShowHistory = showHistory,
            ShowHistoryOutput = showHistoryOutput,
            ShowInput = showInput,
        };

        // Original JSON -> Transitional JSON
        string originalJson = JsonConvert.SerializeObject(original, InProcessJsonSettings);
        GetInstanceStatusOptions? transitional = JsonConvert.DeserializeObject<GetInstanceStatusOptions?>(originalJson, InProcessJsonSettings);
        Assert.NotNull(transitional);
        AssertBackwardsCompatible(original, transitional);

        // Transitional JSON -> Latest JSON
        string transitionalJson = JsonConvert.SerializeObject(transitional, InProcessJsonSettings);
        GetInstanceOptions? latest = System.Text.Json.JsonSerializer.Deserialize<GetInstanceOptions?>(transitionalJson);
        Assert.NotNull(latest);
        Assert.Equal(transitional.GetInputsAndOutputs, latest.GetInputsAndOutputs);

        // Latest JSON -> Transitional JSON
        string latestJson = System.Text.Json.JsonSerializer.Serialize(latest);
        transitional = JsonConvert.DeserializeObject<GetInstanceStatusOptions?>(latestJson, InProcessJsonSettings);
        Assert.NotNull(transitional);
        Assert.Equal(latest.GetInputsAndOutputs, transitional.GetInputsAndOutputs);

        // Transitional JSON -> Original JSON
        transitionalJson = JsonConvert.SerializeObject(transitional, InProcessJsonSettings);
        GetInstanceStatusOptions? roundTrip = JsonConvert.DeserializeObject<GetInstanceStatusOptions?>(transitionalJson, InProcessJsonSettings);
        Assert.NotNull(roundTrip);
        AssertForwardsCompatible(transitional, roundTrip);

        // Compare the round-tripped data with the original data
        Assert.Equal(roundTrip.ShowInput, roundTrip.ShowHistoryOutput); // Always the same now
        Assert.Equal(original.ShowInput || original.ShowHistoryOutput, roundTrip.ShowInput);
        Assert.False(roundTrip.ShowHistory); // Does not exist in the latest data model
    }

    private static void AssertBackwardsCompatible(GetInstanceStatusOptions expected, GetInstanceStatusOptions actual)
    {
        Assert.Equal(actual.ShowInput, actual.ShowHistoryOutput); // Always the same now
        Assert.Equal(expected.ShowInput || expected.ShowHistoryOutput, actual.ShowInput);
        Assert.Equal(expected.ShowHistory, actual.ShowHistory);
    }

    private static void AssertForwardsCompatible(GetInstanceStatusOptions expected, GetInstanceStatusOptions actual)
    {
        Assert.Equal(actual.ShowInput, actual.ShowHistoryOutput); // Always the same now
        Assert.Equal(expected.ShowInput || expected.ShowHistoryOutput, actual.ShowInput);
        Assert.False(actual.ShowHistory); // Does not exist in the latest data model
    }
}
