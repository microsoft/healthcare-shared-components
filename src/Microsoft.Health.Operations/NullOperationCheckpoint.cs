// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Health.Operations;

/// <summary>
/// Represents a default <see cref="IOperationCheckpoint"/>.
/// </summary>
public sealed class NullOperationCheckpoint : IOperationCheckpoint
{
    /// <summary>
    /// Get the <see cref="NullOperationCheckpoint"/> instance.
    /// </summary>
    /// <value>The singleton instance.</value>
    public static NullOperationCheckpoint Value { get; } = new NullOperationCheckpoint();

    /// <inheritdoc cref="IOperationCheckpoint.CreatedTime" />
    public DateTime? CreatedTime => null;

    /// <inheritdoc cref="IOperationCheckpoint.PercentComplete" />
    public int? PercentComplete => 0;

    /// <inheritdoc cref="IOperationCheckpoint.ResourceIds" />
    public IReadOnlyCollection<string>? ResourceIds => null;

    /// <inheritdoc cref="IOperationCheckpoint.AdditionalProperties" />
    public IEnumerable<KeyValuePair<string, string>> AdditionalProperties => Enumerable.Empty<KeyValuePair<string, string>>();

    private NullOperationCheckpoint()
    { }
}
