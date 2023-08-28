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
using System.Collections.Immutable;

namespace Microsoft.Health.Api.Features.HealthChecks;

internal class CachedHealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ValueCache<HealthReport> _healthCheckReportCache;

    private static readonly IImmutableDictionary<HealthStatus, int> DefaultStatusCodesMapping = ImmutableDictionary.CreateRange(
        new KeyValuePair<HealthStatus, int>[]
        {
            KeyValuePair.Create(HealthStatus.Healthy, StatusCodes.Status200OK),
            KeyValuePair.Create(HealthStatus.Degraded, StatusCodes.Status200OK),
            KeyValuePair.Create(HealthStatus.Unhealthy, StatusCodes.Status503ServiceUnavailable),
        });

    public CachedHealthCheckMiddleware(RequestDelegate next, ValueCache<HealthReport> healthCheckReportCache)
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
        HealthReport latestReport = await _healthCheckReportCache.GetCachedData(httpContext.RequestAborted).ConfigureAwait(false);

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
                Status = Enum.GetName(entry.Value.Status),
                entry.Value.Description,
            }),
        };
    }
}
