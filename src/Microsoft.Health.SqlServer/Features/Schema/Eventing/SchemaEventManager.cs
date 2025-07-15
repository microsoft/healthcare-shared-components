// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Features.Schema.Eventing;

internal sealed class SchemaEventManager : ISchemaEventPublisher, ISchemaEventSubscriber
{
    public event EventHandler<SchemaUpgradedEventArgs> SchemaUpgraded;

    public void OnSchemaUpgraded(int version, bool isFullSchemaSnapshot)
        => SchemaUpgraded?.Invoke(this, new SchemaUpgradedEventArgs(version, isFullSchemaSnapshot));
}
