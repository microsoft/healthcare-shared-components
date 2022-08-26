// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Health.Operations;

/// <summary>
/// Represents the state of a long-running operation.
/// </summary>
/// <typeparam name="T">The type used to denote the category of operation.</typeparam>
public sealed class OperationState<T> : IOperationState<T>
{
    /// <inheritdoc cref="IOperationState{T}.OperationId"/>
    public Guid OperationId { get; init; }

    /// <inheritdoc cref="IOperationState{T}.Type"/>
    public T? Type { get; init; }

    /// <inheritdoc cref="IOperationState{T}.CreatedTime"/>
    public DateTime CreatedTime { get; init; }

    /// <inheritdoc cref="IOperationState{T}.LastUpdatedTime"/>
    public DateTime LastUpdatedTime { get; init; }

    /// <inheritdoc cref="IOperationState{T}.Status"/>
    public OperationStatus Status { get; init; }

    /// <inheritdoc cref="IOperationState{T}.PercentComplete"/>
    public int? PercentComplete { get; init; }

    /// <inheritdoc cref="IOperationState{T}.Resources"/>
    public IReadOnlyCollection<Uri>? Resources { get; init; }

    /// <inheritdoc cref="IOperationState{T}.Results"/>
    public object? Results => null;
}

/// <summary>
/// Represents the state and results of a long-running operation
/// </summary>
/// <typeparam name="TType">The type used to denote the category of operation.</typeparam>
/// <typeparam name="TResults">The type used to represent the results of the operation.</typeparam>
public sealed class OperationState<TType, TResults> : IOperationState<TType>
{
    /// <inheritdoc cref="IOperationState{T}.OperationId"/>
    public Guid OperationId { get; init; }

    /// <inheritdoc cref="IOperationState{T}.Type"/>
    public TType? Type { get; init; }

    /// <inheritdoc cref="IOperationState{T}.CreatedTime"/>
    public DateTime CreatedTime { get; init; }

    /// <inheritdoc cref="IOperationState{T}.LastUpdatedTime"/>
    public DateTime LastUpdatedTime { get; init; }

    /// <inheritdoc cref="IOperationState{T}.Status"/>
    public OperationStatus Status { get; init; }

    /// <inheritdoc cref="IOperationState{T}.PercentComplete"/>
    public int? PercentComplete { get; init; }

    /// <inheritdoc cref="IOperationState{T}.Resources"/>
    public IReadOnlyCollection<Uri>? Resources { get; init; }

    /// <summary>
    /// Gets the results of the operation.
    /// </summary>
    /// <remarks>
    /// The results may change over time as the operation continues execution.
    /// </remarks>
    /// <value>
    /// An object whose type depends on the <see cref="OperationState{T}.Type"/> if specified;
    /// otherwise <see langword="null"/>.
    /// </value>
    public TResults? Results { get; init; }

    object? IOperationState<TType>.Results => Results;
}
