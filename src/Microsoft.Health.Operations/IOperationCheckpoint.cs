// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Health.Operations;

/// <summary>
/// Represents the set of data that may be encoded in an operation's checkpoint.
/// </summary>
public interface IOperationCheckpoint
{
    /// <summary>
    /// Gets the optional date and time the operation was started.
    /// </summary>
    /// <value>
    /// The <see cref="DateTime"/> when the operation was started, or <see langword="null"/> if the date and
    /// time may be found outside of the checkpoint.
    /// </value>
    DateTime? CreatedTime { get; }

    /// <summary>
    /// Gets the optional percentage of work that has been completed by the operation.
    /// </summary>
    /// <value>An integer ranging from 0 to 100.</value>
    int? PercentComplete { get; }

    /// <summary>
    /// Gets the optional collection of resources IDs that the operation is creating or manipulating.
    /// </summary>
    /// <remarks>
    /// Not all operations target one or more resources.
    /// </remarks>
    /// <value>A collection of resource IDs, or <see langword="null"/> if there are no targeted resources.</value>
    IReadOnlyCollection<string>? ResourceIds { get; }
}
