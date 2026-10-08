// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Api.Features.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Api.UnitTests.Features.Security;

[SuppressMessage("Design", "MSTEST0032:Assertion condition is always true", Justification = "Validating constants")]
[TestClass]
public class SecurityHeadersHelperTests
{
    [TestMethod]
    public async Task GivenANullContext_WhenSettingSecurityHeaders_ThenExceptionIsThrown()
        => await Assert.ThrowsAsync<ArgumentNullException>(() => SecurityHeadersHelper.SetSecurityHeaders(null));

    [TestMethod]
    public async Task GivenAnIncorrectType_WhenSettingSecurityHeaders_ThenExceptionIsThrown()
    {
        int notAContext = 1;

        await Assert.ThrowsAsync<ArgumentException>(() => SecurityHeadersHelper.SetSecurityHeaders(notAContext));
    }

    [TestMethod]
    public async Task GivenAContext_WhenSettingSecurityHeaders_TheXContentTypeOptionsHeaderIsSet()
    {
        var defaultHttpContext = new DefaultHttpContext();
        await SecurityHeadersHelper.SetSecurityHeaders(defaultHttpContext);

        Assert.IsNotNull(defaultHttpContext.Response.Headers);
        Assert.IsNotEmpty(defaultHttpContext.Response.Headers);
        Assert.AreEqual("X-Content-Type-Options", SecurityHeadersHelper.XContentTypeOptions);
        Assert.IsTrue(defaultHttpContext.Response.Headers.TryGetValue(SecurityHeadersHelper.XContentTypeOptions, out StringValues headerValue));
        Assert.AreEqual<string>("nosniff", headerValue);
    }

    [TestMethod]
    public async Task GivenAContext_WhenSettingSecurityHeaders_TheXFrameOptionsHeaderIsSet()
    {
        var defaultHttpContext = new DefaultHttpContext();
        await SecurityHeadersHelper.SetSecurityHeaders(defaultHttpContext);

        Assert.IsNotNull(defaultHttpContext.Response.Headers);
        Assert.IsNotEmpty(defaultHttpContext.Response.Headers);
        Assert.AreEqual("X-Frame-Options", SecurityHeadersHelper.XFrameOptions);
        Assert.IsTrue(defaultHttpContext.Response.Headers.TryGetValue(SecurityHeadersHelper.XFrameOptions, out StringValues headerValue));
        Assert.AreEqual<string>("SAMEORIGIN", headerValue);
    }

    [TestMethod]
    public async Task GivenAContext_WhenSettingSecurityHeaders_TheContentSecurityPolicyHeaderIsSet()
    {
        var defaultHttpContext = new DefaultHttpContext();
        await SecurityHeadersHelper.SetSecurityHeaders(defaultHttpContext);

        Assert.IsNotNull(defaultHttpContext.Response.Headers);
        Assert.IsNotEmpty(defaultHttpContext.Response.Headers);
        Assert.AreEqual("Content-Security-Policy", SecurityHeadersHelper.ContentSecurityPolicy);
        Assert.IsTrue(defaultHttpContext.Response.Headers.TryGetValue(SecurityHeadersHelper.ContentSecurityPolicy, out StringValues headerValue));
        Assert.AreEqual<string>("frame-src 'self';", headerValue);
    }
}
