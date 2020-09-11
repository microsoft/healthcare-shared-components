// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using MediatR;

namespace Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications
{
    public class SchemaUpgradedNotification : INotification
    {
        public SchemaUpgradedNotification(int version, bool isFullSchemaSnapshotUpgrade)
        {
            EnsureArg.IsGte(version, 1);

            Version = version;
            IsFullSchemaSnapshotUpgrade = isFullSchemaSnapshotUpgrade;
        }

        public int Version { get; }

        public bool IsFullSchemaSnapshotUpgrade { get; }
    }
}
