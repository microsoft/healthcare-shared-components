// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.DurableTask.Client;

namespace Microsoft.Health.Operations.Functions.Worker.DurableTask;

#pragma warning disable CS0618 // Allow the user of obsolete OrchestrationRuntimeStatus values methods

/// <summary>
/// Provides a set of <see langword="static"/> utility methods for <see cref="OrchestrationRuntimeStatus"/> values.
/// </summary>
public static class OrchestrationRuntimeStatusExtensions
{
    /// <summary>
    /// Determines whether the <see cref="OrchestrationRuntimeStatus"/> indicates that the orchestration has not yet finished.
    /// </summary>
    /// <param name="status">The orchestration's reported status.</param>
    /// <returns>
    /// <see langword="true"/> if the orchestration has not yet reached a terminal status; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsInProgress(this OrchestrationRuntimeStatus status)
        => status is OrchestrationRuntimeStatus.Pending or OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.ContinuedAsNew;

    /// <summary>
    /// Determines whether the <see cref="OrchestrationRuntimeStatus"/> indicates that the orchestration has stopped execution.
    /// </summary>
    /// <param name="status">The orchestration's reported status.</param>
    /// <returns>
    /// <see langword="true"/> if the orchestration has reached a terminal status; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsStopped(this OrchestrationRuntimeStatus status)
    {
        return status is OrchestrationRuntimeStatus.Completed
            or OrchestrationRuntimeStatus.Canceled
            or OrchestrationRuntimeStatus.Terminated
            or OrchestrationRuntimeStatus.Failed
            or OrchestrationRuntimeStatus.Suspended;
    }

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
    {
        return status switch
        {
            OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.ContinuedAsNew => OperationStatus.Running,
            OrchestrationRuntimeStatus.Completed => OperationStatus.Succeeded,
            OrchestrationRuntimeStatus.Failed => OperationStatus.Failed,
            OrchestrationRuntimeStatus.Canceled or OrchestrationRuntimeStatus.Terminated => OperationStatus.Canceled,
            OrchestrationRuntimeStatus.Pending => OperationStatus.NotStarted,
            OrchestrationRuntimeStatus.Suspended => OperationStatus.Paused,
            _ => OperationStatus.Unknown
        };
    }

    /// <summary>
    /// Gets the corresponding <see cref="OrchestrationRuntimeStatus"/> value for the given
    /// <see cref="OperationStatus"/> value.
    /// </summary>
    /// <param name="status">The operation's reported status.</param>
    /// <returns>The corresponding <see cref="OrchestrationRuntimeStatus"/> value.</returns>
    public static OrchestrationRuntimeStatus ToOrchestrationRuntimeStatus(this OperationStatus status)
    {
        return status switch
        {
            OperationStatus.NotStarted => OrchestrationRuntimeStatus.Pending,
            OperationStatus.Running => OrchestrationRuntimeStatus.Running,
            OperationStatus.Completed or OperationStatus.Succeeded => OrchestrationRuntimeStatus.Completed,
            OperationStatus.Failed => OrchestrationRuntimeStatus.Failed,
            OperationStatus.Canceled => OrchestrationRuntimeStatus.Terminated,
            OperationStatus.Paused => OrchestrationRuntimeStatus.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
    }
}
