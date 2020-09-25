// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Health.Api.Features.Audit;
using Microsoft.Health.Core.Features.Security;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.Audit
{
    public class AuditEgressMiddlewareTests
    {
        private readonly IClaimsExtractor _claimsExtractor = Substitute.For<IClaimsExtractor>();
        private readonly IAuditHelper _auditHelper = Substitute.For<IAuditHelper>();

        private readonly AuditEgressMiddleware _auditMiddleware;

        private readonly HttpContext _httpContext = new DefaultHttpContext();

        public AuditEgressMiddlewareTests()
        {
            _auditMiddleware = new AuditEgressMiddleware(
                httpContext => Task.CompletedTask,
                _claimsExtractor,
                _auditHelper);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task GivenAuthXFailed_WhenInvoked_ThenAuditLogShouldBeLogged(HttpStatusCode statusCode)
        {
            _httpContext.Response.StatusCode = (int)statusCode;

            await _auditMiddleware.Invoke(_httpContext);

            _auditHelper.Received(1).LogExecuted(_httpContext, _claimsExtractor);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.Gone)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.MethodNotAllowed)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.NotAcceptable)]
        [InlineData(HttpStatusCode.NotModified)]
        [InlineData(HttpStatusCode.PreconditionFailed)]
        [InlineData(HttpStatusCode.RequestEntityTooLarge)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.UnsupportedMediaType)]
        public async Task GivenARequest_WhenInvoked_ThenAuditLogShouldBeLogged(HttpStatusCode statusCode)
        {
            _httpContext.Response.StatusCode = (int)statusCode;

            await _auditMiddleware.Invoke(_httpContext);

            _auditHelper.Received(1).LogExecuted(_httpContext, _claimsExtractor);
        }
    }
}
