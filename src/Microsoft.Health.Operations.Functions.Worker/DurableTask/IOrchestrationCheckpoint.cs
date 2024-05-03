// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.DurableTask;

namespace Microsoft.Health.Operations.Functions.Worker.DurableTask;

/// <summary>
/// Represents the set of data that may be encoded in an orchestration's checkpoint.
/// </summary>
public interface IOrchestrationCheckpoint : IOperationCheckpoint
{
    /// <summary>
    /// Retrieves the results of an orchestration based on the serialied output
    /// and the corresponding data converter.
    /// </summary>
    /// <param name="serializedOutput">The serialized output.</param>
    /// <param name="converter">The converter used to deserialize the output.</param>
    /// <returns>The formatted orchestration results.</returns>
    object? GetResults(string serializedOutput, DataConverter converter);
}
