// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.AspNetCore.Builder;
using Microsoft.Health.Core.Features.Context;

namespace Microsoft.Health.Api.Features.Context
{
    public static class RequestContextAfterAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestContextAfterAuthentication<T>(
            this IApplicationBuilder builder)
            where T : IRequestContext
        {
            EnsureArg.IsNotNull(builder, nameof(builder));

            return builder.UseMiddleware<RequestContextAfterAuthenticationMiddleware<T>>();
        }
    }
}