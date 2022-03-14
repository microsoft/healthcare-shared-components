// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Api.Features.Audit;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AuditEventTypeAttribute : Attribute
{
    public AuditEventTypeAttribute(string auditEventType)
    {
        EnsureArg.IsNotNull(auditEventType, nameof(auditEventType));
        AuditEventType = auditEventType;
    }

    public string AuditEventType { get; }
}
