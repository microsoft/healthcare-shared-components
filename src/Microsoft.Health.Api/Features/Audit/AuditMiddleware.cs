// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Api.Features.Audit
{
    /// <summary>
    /// A middleware that logs executed audit events.
    /// </summary>
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IClaimsExtractor _claimsExtractor;
        private readonly IAuditHelper _auditHelper;
        private readonly IAuditEgressLogger _auditEgressLogger;

        public AuditMiddleware(
            RequestDelegate next,
            IClaimsExtractor claimsExtractor,
            IAuditHelper auditHelper,
            IAuditEgressLogger auditEgressLogger)
        {
            EnsureArg.IsNotNull(next, nameof(next));
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(auditHelper, nameof(auditHelper));
            EnsureArg.IsNotNull(auditEgressLogger, nameof(auditEgressLogger));

            _next = next;
            _claimsExtractor = claimsExtractor;
            _auditHelper = auditHelper;
            _auditEgressLogger = auditEgressLogger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                var statusCode = (HttpStatusCode)context.Response.StatusCode;

                _auditEgressLogger.LogExecuted(context, _claimsExtractor, _auditHelper);
            }
        }
    }
}
