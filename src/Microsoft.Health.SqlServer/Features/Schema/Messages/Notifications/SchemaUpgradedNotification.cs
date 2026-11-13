// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Medino;

namespace Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;

public class SchemaUpgradedNotification : INotification
{
    public SchemaUpgradedNotification(string databaseName, int version, bool isFullSchemaSnapshot)
    {
        EnsureArg.IsNotNullOrEmpty(databaseName);
        EnsureArg.IsGte(version, 1);

        DatabaseName = databaseName;
        Version = version;
        IsFullSchemaSnapshot = isFullSchemaSnapshot;
    }

    public string DatabaseName { get; }

    public int Version { get; }

    public bool IsFullSchemaSnapshot { get; }
}
