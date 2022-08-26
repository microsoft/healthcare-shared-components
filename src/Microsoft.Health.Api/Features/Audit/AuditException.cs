// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Health.Abstractions.Exceptions;

namespace Microsoft.Health.Api.Features.Audit;

public class AuditException : MicrosoftHealthException
{
    public AuditException()
    {
    }

    public AuditException(string message)
        : base(message)
    {
    }

    public AuditException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AuditException(string controllerName, string actionName)
        : base(string.Format(CultureInfo.CurrentCulture, Resources.MissingAuditInformation, controllerName, actionName))
    {
    }
}
