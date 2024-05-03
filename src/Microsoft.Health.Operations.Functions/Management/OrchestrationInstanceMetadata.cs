// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Operations.Functions.Management;

public sealed class OrchestrationInstanceMetadata
{
    public string Name { get; }

    public string InstanceId { get; }

    public OrchestrationRuntimeStatus RuntimeStatus { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastUpdatedAt { get; init; }

    public string? SerializedInput { get; init; }

    public string? SerializedOutput { get; init; }

    public string? SerializedCustomStatus { get; init; }

    public OrchestrationInstanceMetadata(string name, string instanceId)
    {
        Name = EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));
        InstanceId = EnsureArg.IsNotNullOrWhiteSpace(instanceId, nameof(instanceId));
    }
}
