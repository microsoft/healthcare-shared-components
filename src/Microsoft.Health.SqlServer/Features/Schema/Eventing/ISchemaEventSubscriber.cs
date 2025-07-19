// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Features.Schema.Eventing;

/// <summary>
/// Represents a subscriber for schema-related events.
/// </summary>
public interface ISchemaEventSubscriber
{
    /// <summary>
    /// Sets an event handler for receiving information schema upgrades.
    /// </summary>
    event EventHandler<SchemaUpgradedEventArgs> SchemaUpgraded;
}
