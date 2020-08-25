// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Api.Features.Audit
{
    public class AuditHeaderTooLargeException : AuditHeaderException
    {
        public AuditHeaderTooLargeException(string headerName, int size)
            : base(string.Format(Resources.CustomAuditHeaderTooLarge, AuditConstants.MaximumLengthOfCustomHeader, headerName, size))
        {
        }
    }
}
