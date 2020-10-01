// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Api.Features.Audit
{
    /// <summary>
    /// Logger to log executed messages.
    /// </summary>
    public interface IAuditEgressLogger
    {
        /// <summary>
        /// Logs an executed audit entry for the current operation.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <param name="claimsExtractor">The extractor used to extract claims.</param>
        /// <param name="auditHelper">Audit helper.</param>
        void LogExecuted(HttpContext httpContext, IClaimsExtractor claimsExtractor, IAuditHelper auditHelper);
    }
}
