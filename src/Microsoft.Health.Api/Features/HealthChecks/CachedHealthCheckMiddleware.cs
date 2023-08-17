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
using System.Linq;

namespace Microsoft.Health.Api.Features.HealthChecks;

internal class CachedHealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AsyncData<HealthReport> _healthCheckReportCache;

    private static readonly IReadOnlyDictionary<HealthStatus, int> DefaultStatusCodesMapping = new Dictionary<HealthStatus, int>
        {
            {HealthStatus.Healthy, StatusCodes.Status200OK},
            {HealthStatus.Degraded, StatusCodes.Status200OK},
            {HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable},
        };

    public CachedHealthCheckMiddleware(RequestDelegate next, AsyncData<HealthReport> healthCheckReportCache)
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
        HealthReport latestReport = await _healthCheckReportCache.GetCachedData().ConfigureAwait(false);

        if (!DefaultStatusCodesMapping.TryGetValue(latestReport.Status, out var statusCode))
        {
            var message = $"No status code mapping found for {nameof(HealthStatus)} value: {latestReport.Status}.";
            throw new InvalidOperationException(message);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(FormatReport(latestReport)).ConfigureAwait(false);
    }

    private static object FormatReport(HealthReport healthReport)
    {
        return new
        {
            OverallStatus = healthReport.Status.ToString(),
            Details = healthReport.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = Enum.GetName(typeof(HealthStatus), entry.Value.Status),
                Description = entry.Value.Description,
                Data = entry.Value.Data,
            }),
        };
    }
}
