// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.SqlServer.Api.Controllers;
using Microsoft.Health.SqlServer.Api.Features.Filters;
using Microsoft.Health.SqlServer.Api.UnitTests.Controllers;
using Microsoft.Health.SqlServer.Features.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.Api.UnitTests.Features.Filters;

public sealed class HttpExceptionFilterTests : IDisposable
{
    private readonly SchemaController _controller;
    private readonly ActionExecutedContext _context;

    public HttpExceptionFilterTests()
    {
        _controller = new SchemaController(
            new SchemaInformation((int)TestSchemaVersion.Version1, (int)TestSchemaVersion.Version3),
            Substitute.For<IScriptProvider>(),
            Substitute.For<IUrlHelperFactory>(),
            Substitute.For<IMediator>(),
            NullLogger<SchemaController>.Instance);
        _context = new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>(),
            _controller);
    }

    [Fact]
    public void GivenANotImplementedException_WhenExecutingAnAction_ThenTheResponseShouldBeAJsonResultWithNotImplementedStatusCode()
    {
        var filter = new HttpExceptionFilterAttribute();

        _context.Exception = Substitute.For<NotImplementedException>();

        filter.OnActionExecuted(_context);

        var result = _context.Result as JsonResult;

        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.NotImplemented, result.StatusCode);
    }

    [Fact]
    public void GivenANotFoundException_WhenExecutingAnAction_ThenTheResponseShouldBeAJsonResultWithNotFoundStatusCode()
    {
        var filter = new HttpExceptionFilterAttribute();

        _context.Exception = Substitute.For<FileNotFoundException>();

        filter.OnActionExecuted(_context);

        var result = _context.Result as JsonResult;

        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public void GivenASqlRecordNotFoundException_WhenExecutingAnAction_ThenTheResponseShouldBeAJsonResultWithNotFoundStatusCode()
    {
        var filter = new HttpExceptionFilterAttribute();

        _context.Exception = Substitute.For<SqlRecordNotFoundException>("SQL record not found");

        filter.OnActionExecuted(_context);

        var result = _context.Result as JsonResult;

        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public void GivenASqlOperationFailedException_WhenExecutingAnAction_ThenTheResponseShouldBeAJsonResultWithInternalServerErrorAsStatusCode()
    {
        var filter = new HttpExceptionFilterAttribute();

        _context.Exception = Substitute.For<SqlOperationFailedException>("SQL operation failed");

        filter.OnActionExecuted(_context);

        var result = _context.Result as JsonResult;

        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
    }

    public void Dispose()
    {
        _controller.Dispose();
        GC.SuppressFinalize(this);
    }
}
