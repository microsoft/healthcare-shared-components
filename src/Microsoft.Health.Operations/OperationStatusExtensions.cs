// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Operations;

/// <summary>
/// Provides a set of <see langword="static"/> utility methods for <see cref="OperationStatus"/> values.
/// </summary>
public static class OperationStatusExtensions
{
    /// <summary>
    /// Determines whether the <see cref="OperationStatus"/> indicates that the operation has not yet finished.
    /// </summary>
    /// <remarks><see cref="OperationStatus.Unknown"/> is not considered in-progress.</remarks>
    /// <param name="status">The operation's reported status.</param>
    /// <returns>
    /// <see langword="true"/> if the operation has not yet reached a terminal status; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsInProgress(this OperationStatus status)
        => status is OperationStatus.NotStarted or OperationStatus.Running;

    /// <summary>
    /// Determines whether the <see cref="OperationStatus"/> indicates that the operation has stopped execution.
    /// </summary>
    /// <remarks><see cref="OperationStatus.Unknown"/> is not considered stopped.</remarks>
    /// <param name="status">The operation's reported status.</param>
    /// <returns>
    /// <see langword="true"/> if the operation has reached a terminal status; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsStopped(this OperationStatus status)
        => status is OperationStatus.Completed or OperationStatus.Canceled or OperationStatus.Failed;
}
