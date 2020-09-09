﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Health.Core.Features.Security;

namespace Microsoft.Health.Api.Features.Audit
{
    [AttributeUsage(AttributeTargets.Class)]
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

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            AuditHelper.LogExecuted(context.HttpContext, ClaimsExtractor);

            base.OnResultExecuted(context);
        }
    }
}
