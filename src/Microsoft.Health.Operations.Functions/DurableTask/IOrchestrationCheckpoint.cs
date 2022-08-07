// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Operations.Functions.DurableTask;

/// <summary>
/// Represents the set of data that may be encoded in an orchestration's checkpoint.
/// </summary>
public interface IOrchestrationCheckpoint : IOperationCheckpoint
{
    /// <summary>
    /// Retrieves the results of an orchestration based on the optional output
    /// and any data encoded in the checkpoint.
    /// </summary>
    /// <param name="output">The optional orchestration output.</param>
    /// <returns>The formatted orchestration results.</returns>
    object? GetResults(JToken? output);
}
