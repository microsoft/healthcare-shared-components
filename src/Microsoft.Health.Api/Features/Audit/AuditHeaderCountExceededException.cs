// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Microsoft.Health.Api.Features.Audit
{
    public class AuditHeaderCountExceededException : AuditHeaderException
    {
        public AuditHeaderCountExceededException(int size)
            : base(string.Format(CultureInfo.CurrentCulture, Resources.TooManyCustomAuditHeaders, AuditConstants.MaximumNumberOfCustomHeaders, size))
        {
        }
    }
}
