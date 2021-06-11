// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Api.Features.Audit
{
    public class DuplicateActionForAuditEventException : Exception
    {
        public DuplicateActionForAuditEventException()
        {
        }

        public DuplicateActionForAuditEventException(string message)
            : base(message)
        {
        }

        public DuplicateActionForAuditEventException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public DuplicateActionForAuditEventException(string controllerName, string actionName)
            : base(string.Format(Resources.DuplicateActionForAuditEvent, controllerName, actionName))
        {
        }
    }
}
