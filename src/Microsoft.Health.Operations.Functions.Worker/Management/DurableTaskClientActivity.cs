// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Operations.Functions.Management;

namespace Microsoft.Health.Operations.Functions.Worker.Management;

/// <summary>
/// Contains a collection of activities that serve as a proxy for the <see cref="DurableTaskClient"/>.
/// </summary>
public static class DurableTaskClientActivity
{
    /// <summary>
    /// Asynchronously retrieves the status of a given operation ID.
    /// </summary>
    /// <param name="options">The options for fetching the status.</param>
    /// <param name="client">A client for interacting with the durable task framework.</param>
    /// <param name="context">The context for the activity.</param>
    /// <param name="instanceId">The instance ID for the orchestration that invoked the activity.</param>
    /// <param name="cancellationToken">An optional token for cancellation.</param>
    /// <returns>
    /// A task representing the <see cref="GetInstanceAsync"/> operation.
    /// The value of its <see cref="Task{TResult}.Result"/> property contains current status of the desired
    /// operation, if found; otherwise, <see langword="null"/>.
    /// </returns>
    [Function(nameof(GetInstanceAsync))]
    public static async Task<OrchestrationInstanceMetadata?> GetInstanceAsync(
        [ActivityTrigger] GetInstanceOptions options,
        [DurableClient] DurableTaskClient client,
        FunctionContext context,
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNullOrWhiteSpace(instanceId, nameof(instanceId));

        ILogger logger = context.GetLogger(typeof(DurableTaskClientActivity).FullName!);
        logger.LogInformation("Fetching status for orchestration instance ID '{InstanceId}'.", instanceId);
        OrchestrationMetadata? metadata = await client.GetInstanceAsync(instanceId, options.GetInputsAndOutputs, cancellationToken).ConfigureAwait(false);

        return metadata is null
            ? null
            : new OrchestrationInstanceMetadata(metadata.Name, metadata.InstanceId)
            {
                CreatedAt = metadata.CreatedAt,
                LastUpdatedAt = metadata.LastUpdatedAt,
                RuntimeStatus = metadata.RuntimeStatus,
                SerializedCustomStatus = metadata.SerializedCustomStatus,
                SerializedInput = metadata.SerializedInput,
                SerializedOutput = metadata.SerializedOutput,
            };
    }
}
