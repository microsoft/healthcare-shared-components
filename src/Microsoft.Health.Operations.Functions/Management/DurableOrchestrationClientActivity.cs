// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Health.Operations.Functions.Management;

/// <summary>
/// Contains a collection of activities that serve as a proxy for the <see cref="IDurableOrchestrationClient"/>.
/// </summary>
public static class DurableOrchestrationClientActivity
{
    /// <summary>
    /// Asynchronously retrieves the status of a given operation ID.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <param name="client">A client for interacting with the durable task framework.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="GetInstanceStatusAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains current status of the desired
    /// operation, if found; otherwise, <see langword="null"/>.
    /// </returns>
    [FunctionName(nameof(GetInstanceStatusAsync))]
    [Obsolete("Please use GetInstanceAsync instead to help prepare for an isolated worker migration.")]
    public static async Task<DurableOrchestrationMetadata?> GetInstanceStatusAsync(
        [ActivityTrigger] IDurableActivityContext context,
        [DurableClient] IDurableOrchestrationClient client,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Fetching status for orchestration instance ID '{InstanceId}'.", context.InstanceId);

        GetInstanceStatusOptions options = context.GetInput<GetInstanceStatusOptions>();
        DurableOrchestrationStatus? status = await client.GetStatusAsync(context.InstanceId, options.ShowHistory, showHistoryOutput: options.GetInputsAndOutputs, showInput: options.GetInputsAndOutputs);

        return status is null
            ? null
            : new DurableOrchestrationMetadata
            {
                CreatedTime = status.CreatedTime,
                CustomStatus = status.CustomStatus,
                History = status.History,
                Input = status.Input,
                InstanceId = status.InstanceId,
                LastUpdatedTime = status.LastUpdatedTime,
                Name = status.Name,
                Output = status.Output,
                RuntimeStatus = status.RuntimeStatus
            };
    }

    /// <summary>
    /// Asynchronously retrieves the status of a given operation ID.
    /// </summary>
    /// <param name="context">The context for the activity.</param>
    /// <param name="client">A client for interacting with the durable task framework.</param>
    /// <param name="logger">A diagnostic logger.</param>
    /// <returns>
    /// A task representing the <see cref="GetInstanceAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains current status of the desired
    /// operation, if found; otherwise, <see langword="null"/>.
    /// </returns>
    [FunctionName(nameof(GetInstanceAsync))]
    public static async Task<OrchestrationInstanceMetadata?> GetInstanceAsync(
        [ActivityTrigger] IDurableActivityContext context,
        [DurableClient] IDurableOrchestrationClient client,
        ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(logger, nameof(logger));

        logger.LogInformation("Fetching status for orchestration instance ID '{InstanceId}'.", context.InstanceId);

        GetInstanceOptions options = context.GetInput<GetInstanceOptions>();
        DurableOrchestrationStatus? status = await client.GetStatusAsync(context.InstanceId, showHistoryOutput: options.GetInputsAndOutputs, showInput: options.GetInputsAndOutputs);

        return status is null
            ? null
            : new OrchestrationInstanceMetadata(status.Name, status.InstanceId)
            {
                CreatedAt = new DateTimeOffset(status.CreatedTime),
                LastUpdatedAt = new DateTimeOffset(status.LastUpdatedTime),
                RuntimeStatus = status.RuntimeStatus,
                SerializedCustomStatus = status.CustomStatus?.ToString(Formatting.None),
                SerializedInput = status.Input?.ToString(Formatting.None),
                SerializedOutput = status.Output?.ToString(Formatting.None),
            };
    }
}
