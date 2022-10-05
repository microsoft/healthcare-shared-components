// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Health.Api.Features.Security;

internal static class SecurityHeadersHelper
{
    internal const string XContentTypeOptions = "X-Content-Type-Options";
    private const string XContentTypeOptionsValue = "nosniff";

    internal const string XFrameOptions = "X-Frame-Options";
    private const string XFrameOptionsValue = "SAMEORIGIN";

    internal const string ContentSecurityPolicy = "Content-Security-Policy";
    private const string ContentSecurityPolicyValue = "frame-src 'self';";

    internal static Task SetSecurityHeaders(object context)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsTrue(context is HttpContext, nameof(context));
        var httpContext = (HttpContext)context;

        httpContext.Response.Headers.TryAdd(XContentTypeOptions, XContentTypeOptionsValue);

        httpContext.Response.Headers.TryAdd(XFrameOptions, XFrameOptionsValue);
        httpContext.Response.Headers.TryAdd(ContentSecurityPolicy, ContentSecurityPolicyValue);

        return Task.CompletedTask;
    }
}
