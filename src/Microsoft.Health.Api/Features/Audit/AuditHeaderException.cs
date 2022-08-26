// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Abstractions.Exceptions;

namespace Microsoft.Health.Api.Features.Audit;

public class AuditHeaderException : MicrosoftHealthException
{
    public AuditHeaderException()
    {
    }

    public AuditHeaderException(string message)
        : base(message)
    {
    }

    public AuditHeaderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
