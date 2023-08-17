// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using System;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Api.Features.HealthChecks;

public class CachedHealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHealthCheckReportCache _healthCheckReportCache;

    private static readonly IReadOnlyDictionary<string, int> DefaultStatusCodesMapping = new Dictionary<string, int>
        {
            {HealthStatus.Healthy.ToString(), StatusCodes.Status200OK},
            {HealthStatus.Degraded.ToString(), StatusCodes.Status200OK},
            {HealthStatus.Unhealthy.ToString(), StatusCodes.Status503ServiceUnavailable},
        };

    public CachedHealthCheckMiddleware(RequestDelegate next, IHealthCheckReportCache healthCheckReportCache)
    {
        _next = EnsureArg.IsNotNull(next, nameof(next));
        _healthCheckReportCache = EnsureArg.IsNotNull(healthCheckReportCache, nameof(healthCheckReportCache));
    }

    /// <summary>
    /// Processes a request.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        EnsureArg.IsNotNull(httpContext, nameof(httpContext));

        // Get results
        HealthCheckReport latestReport = await _healthCheckReportCache.GetCachedData().ConfigureAwait(false);

        if (!DefaultStatusCodesMapping.TryGetValue(latestReport.OverallStatus, out var statusCode))
        {
            var message = $"No status code mapping found for {nameof(HealthStatus)} value: {latestReport.OverallStatus}.";
            throw new InvalidOperationException(message);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(latestReport).ConfigureAwait(false);
    }
}
