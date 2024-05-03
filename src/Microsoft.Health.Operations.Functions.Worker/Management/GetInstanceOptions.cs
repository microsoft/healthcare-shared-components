// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Microsoft.DurableTask.Client;

namespace Microsoft.Health.Operations.Functions.Worker.Management;

/// <summary>
/// Represents the input to <see cref="DurableTaskClient.GetInstanceAsync(string, bool, CancellationToken)"/>.
/// </summary>
public class GetInstanceOptions
{
    /// <summary>
    /// Gets or sets a flag for including orchestration input and output.
    /// </summary>
    public bool GetInputsAndOutputs { get; set; }
}
