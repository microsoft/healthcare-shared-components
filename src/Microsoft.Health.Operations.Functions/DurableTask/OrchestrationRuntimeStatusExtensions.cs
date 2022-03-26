// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Operations.Functions.DurableTask;

/// <summary>
/// Provides a set of <see langword="static"/> utility methods for <see cref="OrchestrationRuntimeStatus"/> values.
/// </summary>
public static class OrchestrationRuntimeStatusExtensions
{
    /// <summary>
    /// Determines whether the <see cref="OrchestrationRuntimeStatus"/> indicates that the orchestration has not yet finished.
    /// </summary>
    /// <remarks><see cref="OrchestrationRuntimeStatus.Unknown"/> is not considered in-progress.</remarks>
    /// <param name="status">The orchestration's reported status.</param>
    /// <returns>
    /// <see langword="true"/> if the orchestration has not yet reached a terminal status; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsInProgress(this OrchestrationRuntimeStatus status)
        => status is OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.ContinuedAsNew;

    /// <summary>
    /// Determines whether the <see cref="OrchestrationRuntimeStatus"/> indicates that the orchestration has stopped execution.
    /// </summary>
    /// <remarks><see cref="OrchestrationRuntimeStatus.Unknown"/> is not considered stopped.</remarks>
    /// <param name="status">The orchestration's reported status.</param>
    /// <returns>
    /// <see langword="true"/> if the orchestration has reached a terminal status; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsStopped(this OrchestrationRuntimeStatus status)
        => status is OrchestrationRuntimeStatus.Completed
            or OrchestrationRuntimeStatus.Canceled
            or OrchestrationRuntimeStatus.Terminated
            or OrchestrationRuntimeStatus.Failed;

    /// <summary>
    /// Gets the corresponding <see cref="OperationStatus"/> value for the given
    /// <see cref="OrchestrationRuntimeStatus"/> value.
    /// </summary>
    /// <param name="status">The orchestration's reported status.</param>
    /// <returns>
    /// The corresponding <see cref="OperationStatus"/> value or <see cref="OperationStatus.Unknown"/> if the
    /// given <paramref name="status"/> is not recognized.
    /// </returns>
    public static OperationStatus ToOperationStatus(this OrchestrationRuntimeStatus status)
        => status switch
        {
            OrchestrationRuntimeStatus.Running => OperationStatus.Running,
            OrchestrationRuntimeStatus.Completed => OperationStatus.Completed,
            OrchestrationRuntimeStatus.ContinuedAsNew => OperationStatus.Running,
            OrchestrationRuntimeStatus.Failed => OperationStatus.Failed,
            OrchestrationRuntimeStatus.Canceled => OperationStatus.Canceled,
            OrchestrationRuntimeStatus.Terminated => OperationStatus.Canceled,
            OrchestrationRuntimeStatus.Pending => OperationStatus.NotStarted,
            _ => OperationStatus.Unknown
        };
}
