// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Operations.Functions.Management;

namespace Microsoft.Health.Operations.Functions.DurableTask;

/// <summary>
/// Provides a set of <see langword="static"/> utility methods for <see cref="IDurableOrchestrationContext"/>
/// and <see cref="IDurableActivityContext"/> objects.
/// </summary>
public static class IDurableContextExtensions
{
    /// <summary>
    /// Gets the operation ID encoded as the value of the <see cref="IDurableActivityContext.InstanceId"/> property.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <returns>The parsed operation ID.</returns>
    /// <exception cref="FormatException">
    /// The <see cref="IDurableActivityContext.InstanceId"/> cannot be parsed as an operation ID.
    /// </exception>
    public static Guid GetOperationId(this IDurableActivityContext context)
        => GetOperationId(EnsureArg.IsNotNull(context, nameof(context)).InstanceId);

    /// <summary>
    /// Gets the operation ID encoded as the value of the <see cref="IDurableOrchestrationContext.InstanceId"/> property.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <returns>The parsed operation ID.</returns>
    /// <exception cref="FormatException">
    /// The <see cref="IDurableOrchestrationContext.InstanceId"/> cannot be parsed as an operation ID.
    /// </exception>
    public static Guid GetOperationId(this IDurableOrchestrationContext context)
        => GetOperationId(EnsureArg.IsNotNull(context, nameof(context)).InstanceId);

    /// <summary>
    /// Throws a <see cref="FormatException"/> if the value of the <see cref="IDurableOrchestrationContext.InstanceId"/>
    /// property cannot be parsed as an operation ID based on <see cref="OperationId.FormatSpecifier"/>.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// The <see cref="IDurableOrchestrationContext.InstanceId"/> cannot be parsed as an operation ID.
    /// </exception>
    public static void ThrowIfInvalidOperationId(this IDurableOrchestrationContext context)
        => context.GetOperationId();

    /// <summary>
    /// Asynchronously gets the date and time that the orchestration instance was created in UTC.
    /// </summary>
    /// <param name="context">An orchestration context.</param>
    /// <param name="retryOptions">Optional settings for allowing multiple attempts.</param>
    /// <returns>
    /// A task that represents the asynchronous retrieval operation. The value of the <see cref="Task{TResult}.Result"/>
    /// property represents the date and time that the orchestration was created in UTC.
    /// </returns>
    public static async Task<DateTime> GetCreatedTimeAsync(this IDurableOrchestrationContext context, RetryOptions? retryOptions = null)
    {
        // CreatedTime is not preserved between restarts from ContinueAsNew,
        // so this value can be preserved in the input or custom status
        EnsureArg.IsNotNull(context, nameof(context));

        var input = new GetInstanceStatusInput(context.InstanceId, showHistory: false, showHistoryOutput: false, showInput: false);
        DurableOrchestrationStatus status = retryOptions is not null
            ? await context.CallActivityWithRetryAsync<DurableOrchestrationStatus>(nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync), retryOptions, input)
            : await context.CallActivityAsync<DurableOrchestrationStatus>(nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync), input);

        return status.CreatedTime;
    }

    private static Guid GetOperationId(string instanceId)
        => Guid.TryParseExact(instanceId, OperationId.FormatSpecifier, out Guid operationId)
            ? operationId
            : throw new FormatException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidInstanceId, instanceId));
}
