// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Health.Api.Features.Audit;

public class AuditHeaderCountExceededException : AuditHeaderException
{
    public AuditHeaderCountExceededException()
    {
    }

    public AuditHeaderCountExceededException(string message)
        : base(message)
    {
    }

    public AuditHeaderCountExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AuditHeaderCountExceededException(int size)
        : base(string.Format(CultureInfo.CurrentCulture, Resources.TooManyCustomAuditHeaders, AuditConstants.MaximumNumberOfCustomHeaders, size))
    {
    }
}
