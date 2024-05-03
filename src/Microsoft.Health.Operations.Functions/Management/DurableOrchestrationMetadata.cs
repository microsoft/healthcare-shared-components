// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Operations.Functions.Management;

/// <summary>
/// Represents <see cref="DurableOrchestrationStatus"/> with additional, sometimes duplicate, properties
/// to help with the transition to the new Isolated Worker model.
/// </summary>
[Obsolete($"Please use {nameof(OrchestrationInstanceMetadata)} instead.")]
public class DurableOrchestrationMetadata : DurableOrchestrationStatus
{
    public DateTimeOffset CreatedAt
    {
        get => new DateTimeOffset(CreatedTime);
        set => CreatedTime = value.UtcDateTime;
    }

    public DateTimeOffset LastUpdatedAt
    {
        get => new DateTimeOffset(LastUpdatedTime);
        set => LastUpdatedTime = value.UtcDateTime;
    }

    public string? SerializedInput
    {
        get => Input?.ToString(Formatting.None);
        set => Input = string.IsNullOrEmpty(value) ? null : JToken.Parse(value);
    }

    public string? SerializedOutput
    {
        get => Output?.ToString(Formatting.None);
        set => Output = string.IsNullOrEmpty(value) ? null : JToken.Parse(value);
    }

    public string? SerializedCustomStatus
    {
        get => CustomStatus?.ToString(Formatting.None);
        set => CustomStatus = string.IsNullOrEmpty(value) ? null : JToken.Parse(value);
    }
}
