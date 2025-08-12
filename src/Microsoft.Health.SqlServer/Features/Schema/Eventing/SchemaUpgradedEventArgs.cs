// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Schema.Eventing;

/// <summary>
/// Represents the event arguments for when the schema has been upgraded.
/// </summary>
public sealed class SchemaUpgradedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaUpgradedEventArgs"/> class.
    /// </summary>
    /// <param name="version">The version to which the schema was upgraded.</param>
    /// <param name="isFullSchemaSnapshot">Indicates whether the upgrade was a full snapshot.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is less than <c>1</c>.</exception>
    public SchemaUpgradedEventArgs(int version, bool isFullSchemaSnapshot)
    {
        EnsureArg.IsGte(version, 1);

        Version = version;
        IsFullSchemaSnapshot = isFullSchemaSnapshot;
    }

    /// <summary>
    /// Gets the version to which the schema was upgraded.
    /// </summary>
    /// <value>A schema version that is greater than zero.</value>
    public int Version { get; }

    /// <summary>
    /// Gets a value indicating whether the upgrade was a full schema snapshot.
    /// </summary>
    /// <value><see langword="true"/> if the upgrade was a full schema snapshot; otherwise, <see langword="false"/>.</value>
    public bool IsFullSchemaSnapshot { get; }
}
