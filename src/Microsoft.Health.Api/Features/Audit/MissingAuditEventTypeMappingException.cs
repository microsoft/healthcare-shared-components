// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.Api.Features.Audit;

public class MissingAuditEventTypeMappingException : Exception
{
    public MissingAuditEventTypeMappingException()
    {
    }

    public MissingAuditEventTypeMappingException(string message)
        : base(message)
    {
    }

    public MissingAuditEventTypeMappingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MissingAuditEventTypeMappingException(string controllerName, string actionName)
        : base(string.Format(CultureInfo.CurrentCulture, Resources.MissingAuditInformation, controllerName, actionName))
    {
    }
}
