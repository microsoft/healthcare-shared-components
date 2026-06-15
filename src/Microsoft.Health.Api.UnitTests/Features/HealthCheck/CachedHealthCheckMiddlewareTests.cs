// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Health.Api.Features.HealthChecks;
using Microsoft.Health.Core.Features.Health;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

public class CachedHealthCheckMiddlewareTests
{
    [Fact]
    public async Task GivenFreshHealthyReport_WhenInvoked_ThenReturns200()
    {
        ValueCache<HealthReport> cache = new ValueCache<HealthReport>();
        cache.Set(CreateReport(HealthStatus.Healthy));

        DefaultHttpContext httpContext = CreateHttpContext();

        CachedHealthCheckMiddleware middleware = new CachedHealthCheckMiddleware(_ => Task.CompletedTask, cache);
        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("Healthy", await ReadOverallStatusAsync(httpContext));
    }

    [Fact]
    public async Task GivenFreshUnhealthyReport_WhenInvoked_ThenReturns503()
    {
        ValueCache<HealthReport> cache = new ValueCache<HealthReport>();
        cache.Set(CreateReport(HealthStatus.Unhealthy));

        DefaultHttpContext httpContext = CreateHttpContext();

        CachedHealthCheckMiddleware middleware = new CachedHealthCheckMiddleware(_ => Task.CompletedTask, cache);
        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task GivenStalePublishedReport_WhenInvoked_ThenReturns503AndUnhealthy()
    {
        // Reproduces the production scenario where the publisher last published a Healthy report long ago
        // and stopped publishing new reports because each iteration is timing out / failing. With ValueCache
        // expiry, the stale report is no longer served and the middleware surfaces an Unhealthy 503.
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ValueCache<HealthReport> cache = new ValueCache<HealthReport>(TimeSpan.FromMinutes(5), timeProvider);
        cache.Set(CreateReport(HealthStatus.Healthy));

        timeProvider.Advance(TimeSpan.FromHours(6));

        DefaultHttpContext httpContext = CreateHttpContext();

        CachedHealthCheckMiddleware middleware = new CachedHealthCheckMiddleware(_ => Task.CompletedTask, cache);
        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        Assert.Equal("Unhealthy", await ReadOverallStatusAsync(httpContext));
    }

    private static HealthReport CreateReport(HealthStatus status)
    {
        return new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["TestCheck"] = new HealthReportEntry(status, description: "test", duration: TimeSpan.Zero, exception: null, data: null),
            },
            TimeSpan.FromMilliseconds(1));
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<string> ReadOverallStatusAsync(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using JsonDocument doc = await JsonDocument.ParseAsync(httpContext.Response.Body, cancellationToken: CancellationToken.None);
        foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, "OverallStatus", StringComparison.OrdinalIgnoreCase))
            {
                return prop.Value.GetString();
            }
        }

        return null;
    }
}
