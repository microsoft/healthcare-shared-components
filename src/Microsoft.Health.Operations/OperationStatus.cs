// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Operations;

/// <summary>
/// Specifies the status of a long-running operation.
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// Specifies a status that is missing or unrecognized.
    /// </summary>
    Unknown,

    /// <summary>
    /// Specifies a status where execution is pending.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Specifies a status where the operation is executing and has not yet finished.
    /// </summary>
    Running,

    /// <summary>
    /// Specifies a status where the operation has finished successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Specifies a status where the operation has stopped prematurely after encountering one or more errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Specifies a status where the operation has stopped prematurely after being told to do so by the user.
    /// </summary>
    Canceled,
}
