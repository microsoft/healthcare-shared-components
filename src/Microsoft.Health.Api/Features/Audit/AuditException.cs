// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Abstractions.Exceptions;

namespace Microsoft.Health.Api.Features.Audit
{
    public class AuditException : MicrosoftHealthException
    {
        public AuditException(string controllerName, string actionName)
            : base(string.Format(Resources.MissingAuditInformation, controllerName, actionName))
        {
        }
    }
}
