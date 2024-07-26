// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Health.Operations.Serialization;

namespace Microsoft.Health.Operations;

/// <summary>
/// Represents the state of a long-running operation.
/// </summary>
/// <typeparam name="T">The type used to denote the category of operation.</typeparam>
public interface IOperationState<T>
{
    /// <summary>
    /// Gets the operation ID.
    /// </summary>
    /// <value>The unique ID that denotes a particular operation.</value>
    [JsonConverter(typeof(OperationIdJsonConverter))]
    Guid OperationId { get; }

    /// <summary>
    /// Gets the category of the operation.
    /// </summary>
    /// <value>The operation category of type <typeparamref name="T"/>.</value>
    T? Type { get; }

    /// <summary>
    /// Gets the date and time the operation was started.
    /// </summary>
    /// <value>The <see cref="DateTimeOffset"/> when the operation was started.</value>
    [JsonConverter(typeof(UtcCompatibilityJsonConverter))]
    DateTimeOffset CreatedTime { get; }

    /// <summary>
    /// Gets the last date and time the state was updated.
    /// </summary>
    /// <value>The last <see cref="DateTimeOffset"/> when the operation state was updated.</value>
    [JsonConverter(typeof(UtcCompatibilityJsonConverter))]
    DateTimeOffset LastUpdatedTime { get; }

    /// <summary>
    /// Gets the execution status of the operation.
    /// </summary>
    /// <value>The current <see cref="OperationStatus"/>.</value>
    OperationStatus Status { get; }

    /// <summary>
    /// Gets the percentage of work that has been completed by the operation.
    /// </summary>
    /// <value>An optional integer ranging from 0 to 100 if supported.</value>
    int? PercentComplete { get; }

    /// <summary>
    /// Gets the optional collection of resources locations that the operation is creating or manipulating.
    /// </summary>
    /// <remarks>
    /// The set of resources may change until the <see cref="Status"/> indicates completion.
    /// </remarks>
    /// <value>A collection of resource IDs, or <see langword="null"/> if there are no targeted resources.</value>
    IReadOnlyCollection<Uri>? Resources { get; }

    /// <summary>
    /// Gets the optional results of the operation.
    /// </summary>
    /// <remarks>
    /// The results may change over time as the operation continues execution.
    /// </remarks>
    /// <value>
    /// An object whose type depends on the <see cref="OperationState{T}.Type"/> if specified;
    /// otherwise <see langword="null"/>.
    /// </value>
    object? Results { get; }
}
