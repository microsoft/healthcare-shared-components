// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.Api.Features.Audit;

public class AuditHeaderTooLargeException : AuditHeaderException
{
    public AuditHeaderTooLargeException()
    {
    }

    public AuditHeaderTooLargeException(string message)
        : base(message)
    {
    }

    public AuditHeaderTooLargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AuditHeaderTooLargeException(string headerName, int size)
        : base(string.Format(CultureInfo.CurrentCulture, Resources.CustomAuditHeaderTooLarge, AuditConstants.MaximumLengthOfCustomHeader, headerName, size))
    {
    }
}
