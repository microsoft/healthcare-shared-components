// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
            OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.ContinuedAsNew => OperationStatus.Running,
            OrchestrationRuntimeStatus.Completed => OperationStatus.Succeeded,
            OrchestrationRuntimeStatus.Failed => OperationStatus.Failed,
            OrchestrationRuntimeStatus.Canceled or OrchestrationRuntimeStatus.Terminated => OperationStatus.Canceled,
            OrchestrationRuntimeStatus.Pending => OperationStatus.NotStarted,
            _ => OperationStatus.Unknown
        };

    /// <summary>
    /// Gets the corresponding <see cref="OrchestrationRuntimeStatus"/> value for the given
    /// <see cref="OperationStatus"/> value.
    /// </summary>
    /// <param name="status">The operation's reported status.</param>
    /// <returns>
    /// The corresponding <see cref="OrchestrationRuntimeStatus"/> value or <see cref="OrchestrationRuntimeStatus.Unknown"/>
    /// if the given <paramref name="status"/> is not recognized.
    /// </returns>
    public static OrchestrationRuntimeStatus ToOrchestrationRuntimeStatus(this OperationStatus status)
        => status switch
        {
            OperationStatus.NotStarted => OrchestrationRuntimeStatus.Pending,
            OperationStatus.Running => OrchestrationRuntimeStatus.Running,
            OperationStatus.Succeeded => OrchestrationRuntimeStatus.Completed,
            OperationStatus.Failed => OrchestrationRuntimeStatus.Failed,
            OperationStatus.Canceled => OrchestrationRuntimeStatus.Canceled,
            _ => OrchestrationRuntimeStatus.Unknown,
        };

    /// <summary>
    /// Gets the corresponding <see cref="OrchestrationRuntimeStatus"/> values for the given
    /// <see cref="OperationStatus"/> value.
    /// </summary>
    /// <remarks>
    /// There may be more than one corresponding <see cref="OrchestrationRuntimeStatus"/> value
    /// for a given <see cref="OperationStatus"/> value.
    /// </remarks>
    /// <param name="status">The operation's reported status.</param>
    /// <returns>
    /// The corresponding <see cref="OrchestrationRuntimeStatus"/> value or <see cref="OrchestrationRuntimeStatus.Unknown"/>
    /// if the given <paramref name="status"/> is not recognized.
    /// </returns>
    public static IEnumerable<OrchestrationRuntimeStatus> ToOrchestrationRuntimeStatuses(this OperationStatus status)
    {
        switch (status)
        {
            case OperationStatus.NotStarted:
                yield return OrchestrationRuntimeStatus.Pending;
                break;
            case OperationStatus.Running:
                yield return OrchestrationRuntimeStatus.Running;
                yield return OrchestrationRuntimeStatus.ContinuedAsNew;
                break;
            case OperationStatus.Succeeded:
                yield return OrchestrationRuntimeStatus.Completed;
                break;
            case OperationStatus.Failed:
                yield return OrchestrationRuntimeStatus.Failed;
                break;
            case OperationStatus.Canceled:
                yield return OrchestrationRuntimeStatus.Canceled;
                yield return OrchestrationRuntimeStatus.Terminated;
                break;
            default:
                yield return OrchestrationRuntimeStatus.Unknown;
                break;
        }
    }
}
