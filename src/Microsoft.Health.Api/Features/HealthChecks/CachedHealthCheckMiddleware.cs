// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Api.Features.HealthChecks;

internal class CachedHealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ValueCache<HealthReport> _healthCheckReportCache;

    private static readonly ImmutableDictionary<HealthStatus, int> DefaultStatusCodesMapping = ImmutableDictionary.CreateRange(
        new[]
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

    public async Task InvokeAsync(HttpContext httpContext)
    {
        EnsureArg.IsNotNull(httpContext, nameof(httpContext));

        // Get the latest published HealthReport. ValueCache returns null if the report has gone stale
        // (i.e., no fresh report has been published within the configured expiry). When that happens we
        // surface an Unhealthy response rather than continuing to serve a stale report indefinitely.
        HealthReport latestReport = await _healthCheckReportCache.GetAsync(httpContext.RequestAborted).ConfigureAwait(false);

        HealthStatus overallStatus = latestReport?.Status ?? HealthStatus.Unhealthy;

        if (!DefaultStatusCodesMapping.TryGetValue(overallStatus, out var statusCode))
        {
            var message = $"No status code mapping found for {nameof(HealthStatus)} value: {overallStatus}.";
            throw new InvalidOperationException(message);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(FormatReport(latestReport)).ConfigureAwait(false);
    }

    private static object FormatReport(HealthReport healthReport)
    {
        if (healthReport == null)
        {
            return new
            {
                OverallStatus = HealthStatus.Unhealthy.ToString(),
                Details = Enumerable.Empty<object>(),
            };
        }

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
