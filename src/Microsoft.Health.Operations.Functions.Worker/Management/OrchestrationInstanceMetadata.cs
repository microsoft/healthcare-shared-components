// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.DurableTask.Client;

namespace Microsoft.Health.Operations.Functions.Management;

/// <inheritdoc cref="OrchestrationMetadata" />
public sealed class OrchestrationInstanceMetadata
{
    /// <inheritdoc cref="OrchestrationMetadata.Name" />
    public string Name { get; }

    /// <inheritdoc cref="OrchestrationMetadata.InstanceId" />
    public string InstanceId { get; }

    /// <inheritdoc cref="OrchestrationMetadata.RuntimeStatus" />
    public OrchestrationRuntimeStatus RuntimeStatus { get; init; }

    /// <inheritdoc cref="OrchestrationMetadata.CreatedAt" />
    public DateTimeOffset CreatedAt { get; init; }

    /// <inheritdoc cref="OrchestrationMetadata.LastUpdatedAt" />
    public DateTimeOffset LastUpdatedAt { get; init; }

    /// <inheritdoc cref="OrchestrationMetadata.SerializedInput" />
    public string? SerializedInput { get; init; }

    /// <inheritdoc cref="OrchestrationMetadata.SerializedOutput" />
    public string? SerializedOutput { get; init; }

    /// <inheritdoc cref="OrchestrationMetadata.SerializedCustomStatus" />
    public string? SerializedCustomStatus { get; init; }

    /// <inheritdoc cref="OrchestrationMetadata(string, string)" />
    public OrchestrationInstanceMetadata(string name, string instanceId)
    {
        Name = EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));
        InstanceId = EnsureArg.IsNotNullOrWhiteSpace(instanceId, nameof(instanceId));
    }
}
