// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Health.Operations;

/// <summary>
/// Represents the metadata for a long-running DICOM operation.
/// </summary>
/// <typeparam name="T">The type used to denote the operation <see cref="Type"/>.</typeparam>
public class OperationStatus<T>
{
    /// <summary>
    /// Gets or sets the operation ID.
    /// </summary>
    /// <value>The unique ID that denotes a particular operation.</value>
    public Guid OperationId { get; init; }

    /// <summary>
    /// Gets or sets the category of the operation.
    /// </summary>
    public T? Type { get; init; }

    /// <summary>
    /// Gets or sets the date and time the operation was started.
    /// </summary>
    /// <value>The <see cref="DateTime"/> when the operation was started.</value>
    public DateTime CreatedTime { get; init; }

    /// <summary>
    /// Gets or sets the last date and time the operation's execution status was updated.
    /// </summary>
    /// <value>The last <see cref="DateTime"/> when the operation status was updated.</value>
    public DateTime LastUpdatedTime { get; init; }

    /// <summary>
    /// Gets or sets the execution status of the operation.
    /// </summary>
    public OperationRuntimeStatus Status { get; init; }

    /// <summary>
    /// Gets the percentage of work that has been completed by the operation.
    /// </summary>
    /// <value>An integer ranging from 0 to 100.</value>
    public int PercentComplete { get; init; }

    /// <summary>
    /// Gets the optional collection of resources locations that the operation is creating or manipulating.
    /// </summary>
    /// <remarks>
    /// The set of resources may change until the <see cref="Status"/> indicates completion.
    /// </remarks>
    /// <value>A collection of resource IDs, or <see langword="null"/> if there are no targeted resources.</value>
    public IReadOnlyCollection<Uri>? Resources { get; init; }
}
