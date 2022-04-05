// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Operations.Functions.Management;

/// <summary>
/// Represents the input to <see cref="IDurableOrchestrationClient.GetStatusAsync(string, bool, bool, bool)"/>.
/// </summary>
public class GetInstanceStatusOptions
{
    /// <summary>
    /// Gets or sets a flag for including execution history in the response.
    /// </summary>
    public bool ShowHistory { get; }

    /// <summary>
    /// Gets or sets a flag for including input and output in the execution history response.
    /// </summary>
    public bool ShowHistoryOutput { get; }

    /// <summary>
    /// Gets or sets a flag for including the orchestration input.
    /// </summary>
    public bool ShowInput { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetInstanceStatusOptions"/> class based on the provided arguments.
    /// </summary>
    /// <param name="showHistory">Indicates whether execution history should be included in the response.</param>
    /// <param name="showHistoryOutput">Indicates whether the input and output should be included in the execution history response.</param>
    /// <param name="showInput">Indicates whether the orchestration input should be included.</param>
    public GetInstanceStatusOptions(bool showHistory = false, bool showHistoryOutput = false, bool showInput = true)
    {
        ShowHistory = showHistory;
        ShowHistoryOutput = showHistoryOutput;
        ShowInput = showInput;
    }
}
