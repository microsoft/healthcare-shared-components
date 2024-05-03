// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Operations.Functions.DurableTask;

/// <summary>
/// Represents a default <see cref="IOperationCheckpoint"/>.
/// </summary>
public sealed class NullOrchestrationCheckpoint : IOrchestrationCheckpoint
{
    /// <summary>
    /// Get the <see cref="NullOrchestrationCheckpoint"/> instance.
    /// </summary>
    /// <value>The singleton instance.</value>
    public static NullOrchestrationCheckpoint Value { get; } = new NullOrchestrationCheckpoint();

    DateTime? IOperationCheckpoint.CreatedTime => null;

    /// <inheritdoc cref="IOperationCheckpoint.CreatedAtTime" />
    public DateTimeOffset? CreatedAtTime => null;

    /// <inheritdoc cref="IOperationCheckpoint.PercentComplete" />
    public int? PercentComplete => 0;

    /// <inheritdoc cref="IOperationCheckpoint.ResourceIds" />
    public IReadOnlyCollection<string>? ResourceIds => null;

    private NullOrchestrationCheckpoint()
    { }

    /// <inheritdoc cref="IOrchestrationCheckpoint.GetResults(JToken?)" />
    object? IOrchestrationCheckpoint.GetResults(JToken? output)
        => null;
}
