// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Operations.Functions.Management;

/// <summary>
/// Represents the input to the <see cref="DurableOrchestrationClientActivity.GetInstanceAsync" /> method as part of the new isolated worker APIs.
/// </summary>
public class GetInstanceOptions
{
    /// <summary>
    /// Gets or sets a flag for including orchestration input and output.
    /// </summary>
    public bool GetInputsAndOutputs { get; set; }
}
