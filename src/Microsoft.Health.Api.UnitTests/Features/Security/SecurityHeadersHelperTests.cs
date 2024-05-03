// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Api.Features.Security;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.Security;

public class SecurityHeadersHelperTests
{
    [Fact]
    public async Task GivenANullContext_WhenSettingSecurityHeaders_ThenExceptionIsThrown()
        => await Assert.ThrowsAsync<ArgumentNullException>(() => SecurityHeadersHelper.SetSecurityHeaders(null));

    [Fact]
    public async Task GivenAnIncorrectType_WhenSettingSecurityHeaders_ThenExceptionIsThrown()
    {
        int notAContext = 1;

        await Assert.ThrowsAsync<ArgumentException>(() => SecurityHeadersHelper.SetSecurityHeaders(notAContext));
    }

    [Fact]
    public async Task GivenAContext_WhenSettingSecurityHeaders_TheXContentTypeOptionsHeaderIsSet()
    {
        var defaultHttpContext = new DefaultHttpContext();
        await SecurityHeadersHelper.SetSecurityHeaders(defaultHttpContext);

        Assert.NotNull(defaultHttpContext.Response.Headers);
        Assert.NotEmpty(defaultHttpContext.Response.Headers);
        Assert.Equal("X-Content-Type-Options", SecurityHeadersHelper.XContentTypeOptions);
        Assert.True(defaultHttpContext.Response.Headers.TryGetValue(SecurityHeadersHelper.XContentTypeOptions, out StringValues headerValue));
        Assert.Equal("nosniff", headerValue);
    }

    [Fact]
    public async Task GivenAContext_WhenSettingSecurityHeaders_TheXFrameOptionsHeaderIsSet()
    {
        var defaultHttpContext = new DefaultHttpContext();
        await SecurityHeadersHelper.SetSecurityHeaders(defaultHttpContext);

        Assert.NotNull(defaultHttpContext.Response.Headers);
        Assert.NotEmpty(defaultHttpContext.Response.Headers);
        Assert.Equal("X-Frame-Options", SecurityHeadersHelper.XFrameOptions);
        Assert.True(defaultHttpContext.Response.Headers.TryGetValue(SecurityHeadersHelper.XFrameOptions, out StringValues headerValue));
        Assert.Equal("SAMEORIGIN", headerValue);
    }

    [Fact]
    public async Task GivenAContext_WhenSettingSecurityHeaders_TheContentSecurityPolicyHeaderIsSet()
    {
        var defaultHttpContext = new DefaultHttpContext();
        await SecurityHeadersHelper.SetSecurityHeaders(defaultHttpContext);

        Assert.NotNull(defaultHttpContext.Response.Headers);
        Assert.NotEmpty(defaultHttpContext.Response.Headers);
        Assert.Equal("Content-Security-Policy", SecurityHeadersHelper.ContentSecurityPolicy);
        Assert.True(defaultHttpContext.Response.Headers.TryGetValue(SecurityHeadersHelper.ContentSecurityPolicy, out StringValues headerValue));
        Assert.Equal("frame-src 'self';", headerValue);
    }
}
