// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Features.Schema.Eventing;

/// <summary>
/// Represents a publisher for schema-related events.
/// </summary>
public interface ISchemaEventPublisher
{
    /// <summary>
    /// Notifies subscribers that the schema has been upgraded.
    /// </summary>
    /// <param name="version">The version to which the schema was upgraded.</param>
    /// <param name="isFullSchemaSnapshot">Indicates whether the upgrade was a full snapshot.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is less than <c>1</c>.</exception>
    void OnSchemaUpgraded(int version, bool isFullSchemaSnapshot);
}
