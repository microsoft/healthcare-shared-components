﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;

namespace Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications
{
    public class SchemaUpgraded : INotification
    {
        public SchemaUpgraded(int? version)
        {
            Version = version;
        }

        public int? Version { get; }
    }
}
