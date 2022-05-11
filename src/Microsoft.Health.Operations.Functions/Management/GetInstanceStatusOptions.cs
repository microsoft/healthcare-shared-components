﻿// -------------------------------------------------------------------------------------------------
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
    public bool ShowHistory { get; set; }

    /// <summary>
    /// Gets or sets a flag for including input and output in the execution history response.
    /// </summary>
    public bool ShowHistoryOutput { get; set; }

    /// <summary>
    /// Gets or sets a flag for including the orchestration input.
    /// </summary>
    public bool ShowInput { get; set; }
}
