// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Exceptions;

namespace Microsoft.Health.Api.Features.Audit
{
    public class AuditHeaderException : HealthException
    {
        public AuditHeaderException(string headerName, int size)
            : base(string.Format(Resources.CustomAuditHeaderTooLarge, AuditConstants.MaximumLengthOfCustomHeader, headerName, size))
        {
        }

        public AuditHeaderException(int size)
            : base(string.Format(Resources.TooManyCustomAuditHeaders, AuditConstants.MaximumNumberOfCustomHeaders, size))
        {
        }
    }
}
