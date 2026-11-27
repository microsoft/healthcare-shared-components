// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Features.Security;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.Audit;

public class AuditLoggingFilterAttributeTests
{
    [Fact]
    public void GivenNoTimerInHttpContextItems_WhenOnResultExecuted_ThenNoExceptionIsThrown()
    {
        IClaimsExtractor claimsExtractor = Substitute.For<IClaimsExtractor>();
        IAuditHelper auditHelper = Substitute.For<IAuditHelper>();
        var auditLoggingFilterAttribute = new AuditLoggingFilterAttribute(claimsExtractor, auditHelper);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var resultExecutedContext = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new EmptyResult(), controller: null);

        var exception = Record.Exception(() => auditLoggingFilterAttribute.OnResultExecuted(resultExecutedContext));

        Assert.Null(exception);
    }

    [Fact]
    public void GivenTimerAddedOnActionExecuting_WhenOnResultExecuted_ThenDurationIsLoggedFromTimer()
    {
        IClaimsExtractor claimsExtractor = Substitute.For<IClaimsExtractor>();
        IAuditHelper auditHelper = Substitute.For<IAuditHelper>();
        var auditLoggingFilterAttribute = new AuditLoggingFilterAttribute(claimsExtractor, auditHelper);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var actionExecutingContext = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller: null);
        var resultExecutedContext = new ResultExecutedContext(actionContext, new List<IFilterMetadata>(), new EmptyResult(), controller: null);

        auditLoggingFilterAttribute.OnActionExecuting(actionExecutingContext);

        object timer = httpContext.Items["timer"];
        Assert.IsType<Stopwatch>(timer);

        Thread.Sleep(2);

        auditLoggingFilterAttribute.OnResultExecuted(resultExecutedContext);

        auditHelper.Received(1).LogExecuted(httpContext, claimsExtractor, Arg.Any<bool>(), Arg.Is<long?>(d => d.HasValue && d.Value > 0));
    }
}
