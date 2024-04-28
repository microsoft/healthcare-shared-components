// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Operations.Functions.Management;

/// <summary>
/// Represents the input to <see cref="IDurableOrchestrationClient.GetStatusAsync(string, bool, bool, bool)"/>.
/// </summary>
[Obsolete($"Please use {nameof(GetInstanceOptions)} instead.")]
public class GetInstanceStatusOptions
{
    /// <summary>
    /// Gets or sets a flag for including execution history in the response.
    /// </summary>
    public bool ShowHistory { get; set; }

    /// <summary>
    /// Gets or sets a flag for including input and output in the execution history response.
    /// </summary>
    public bool ShowHistoryOutput { get; set; }

    /// <summary>
    /// Gets or sets a flag for including the orchestration input.
    /// </summary>
    public bool ShowInput { get; set; }

    /// <summary>
    /// Gets or sets a flag for including orchestration input and output.
    /// </summary>
    public bool GetInputsAndOutputs
    {
        get => ShowInput || ShowHistoryOutput;
        set
        {
            // The setters and getters aren't symmetrical, but we can make peace with it
            // as long as the code stops using the ShowHistoryOutput property.
            // The property will only be left to handle the deserialization of old data.
            ShowInput = value;
            ShowHistoryOutput = value;
        }
    }
}
