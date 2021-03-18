// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Api.Features.Audit
{
    [AttributeUsage(AttributeTargets.Class)]
    [SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "Derived classes will choose what to expose publicly.")]
    [SuppressMessage("Performance", "CA1813:Avoid unsealed attributes", Justification = "This attribute is meant to be extended.")]
    public class AuditLoggingFilterAttribute : ActionFilterAttribute
    {
        public AuditLoggingFilterAttribute(
            IClaimsExtractor claimsExtractor,
            IAuditHelper auditHelper)
        {
            EnsureArg.IsNotNull(claimsExtractor, nameof(claimsExtractor));
            EnsureArg.IsNotNull(auditHelper, nameof(auditHelper));

            ClaimsExtractor = claimsExtractor;
            AuditHelper = auditHelper;
        }

        protected IClaimsExtractor ClaimsExtractor { get; }

        protected IAuditHelper AuditHelper { get; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            AuditHelper.LogExecuting(context.HttpContext, ClaimsExtractor);

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Log executed messages when the request has completed executing.
        /// This cannot be moved to AuditMidddleware because of the way how batch statements are handled in FHIR.
        /// If log executed is moved to AuditMiddleware, we will end up with just one executed message per batch
        /// instead of one executed message per request in batch.
        /// </summary>
        /// <param name="context">Result executed context.</param>
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            AuditHelper.LogExecuted(context.HttpContext, ClaimsExtractor);

            base.OnResultExecuted(context);
        }
    }
}
