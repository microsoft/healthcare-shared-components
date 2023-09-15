// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Health.Operations.Functions.Worker.Management;

namespace Microsoft.Health.Operations.Functions.Worker.DurableTask;

/// <summary>
/// Provides a set of <see langword="static"/> utility methods for <see cref="TaskOrchestrationContext"/>
/// and <see cref="TaskActivityContext "/> objects.
/// </summary>
public static class TaskContextExtensions
{
    /// <summary>
    /// Gets the operation ID encoded as the value of the <see cref="TaskActivityContext.InstanceId"/> property.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <returns>The parsed operation ID.</returns>
    /// <exception cref="FormatException">
    /// The <see cref="TaskActivityContext.InstanceId"/> cannot be parsed as an operation ID.
    /// </exception>
    public static Guid GetOperationId(this TaskActivityContext context)
        => GetOperationId(EnsureArg.IsNotNull(context, nameof(context)).InstanceId);

    /// <summary>
    /// Gets the operation ID encoded as the value of the <see cref="TaskOrchestrationContext.InstanceId"/> property.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <returns>The parsed operation ID.</returns>
    /// <exception cref="FormatException">
    /// The <see cref="TaskOrchestrationContext.InstanceId"/> cannot be parsed as an operation ID.
    /// </exception>
    public static Guid GetOperationId(this TaskOrchestrationContext context)
        => GetOperationId(EnsureArg.IsNotNull(context, nameof(context)).InstanceId);

    /// <summary>
    /// Throws a <see cref="FormatException"/> if the value of the <see cref="TaskOrchestrationContext.InstanceId"/>
    /// property cannot be parsed as an operation ID based on <see cref="OperationId.FormatSpecifier"/>.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// The <see cref="TaskOrchestrationContext.InstanceId"/> cannot be parsed as an operation ID.
    /// </exception>
    public static void ThrowIfInvalidOperationId(this TaskOrchestrationContext context)
        => context.GetOperationId();

    /// <summary>
    /// Asynchronously gets the date and time that the orchestration instance was created in UTC.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <param name="taskOptions">Optional options for running the task.</param>
    /// <returns>
    /// A task that represents the asynchronous retrieval operation. The value of the <see cref="Task{TResult}.Result"/>
    /// property represents the date and time that the orchestration was created in UTC.
    /// </returns>
    public static async Task<DateTimeOffset> GetCreatedAtTimeAsync(this TaskOrchestrationContext context, TaskOptions? taskOptions = null)
    {
        // CreatedTime is not preserved between restarts from ContinueAsNew,
        // so this value can be preserved in the input or custom status
        EnsureArg.IsNotNull(context, nameof(context));

        var input = new GetInstanceOptions { GetInputsAndOutputs = false, };
        OrchestrationMetadata? metadata = await context.CallActivityAsync<OrchestrationMetadata?>(nameof(DurableTaskClientActivity.GetInstanceAsync), input, taskOptions);

        return metadata!.CreatedAt;
    }

    private static Guid GetOperationId(string instanceId)
        => Guid.TryParseExact(instanceId, OperationId.FormatSpecifier, out Guid operationId)
            ? operationId
            : throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidInstanceId, instanceId));
}
