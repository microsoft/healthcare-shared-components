// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Api.Features.HealthChecks;
using Microsoft.Health.Api.Modules;
using Microsoft.Health.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

public class CachedHealthCheckTests
{
    public CachedHealthCheckTests()
    {
        _healthCheck = Substitute.For<IHealthCheck>();
        _healthCheck.CheckHealthAsync(default, default).ReturnsForAnyArgs(HealthCheckResult.Healthy());

        ServiceCollection services = [];
        services
            .AddHealthChecks()
            .AddCheck(HealthCheckName, _healthCheck, default, default);

        services
            .AddLogging()
            .Configure<HealthCheckCachingOptions>(o =>
            {
                o.Expiry = TimeSpan.FromHours(1);
                o.MaxRefreshThreads = 1;
                o.MinimumCachedHealthStatus = HealthStatus.Unhealthy;
            })
            .RegisterModule<HealthCheckModule>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private const string HealthCheckName = "unit-test";
    private readonly IHealthCheck _healthCheck;
    private readonly ServiceProvider _serviceProvider;

    [Fact]
    public async Task GivenCachedHealthCheck_WhenRunningHealthCheckService_ThenPreserveCacheBetweenInvocations()
    {
        HealthCheckService service = _serviceProvider.GetRequiredService<HealthCheckService>();

        using CancellationTokenSource cts = new();

        AssertHealthReport(await service.CheckHealthAsync(cts.Token));
        await _healthCheck.ReceivedWithAnyArgs(1).CheckHealthAsync(default, default);

        // Attempt to concurrent invoke it multiple times
        HealthReport[] reports = await Task.WhenAll(
            service.CheckHealthAsync(cts.Token),
            service.CheckHealthAsync(cts.Token),
            service.CheckHealthAsync(cts.Token));

        Assert.All(reports, AssertHealthReport);
        await _healthCheck.ReceivedWithAnyArgs(1).CheckHealthAsync(default, default);
    }

    private static void AssertHealthReport(HealthReport actual)
    {
        Assert.Equal(HealthStatus.Healthy, actual.Status);
        Assert.True(actual.Entries.TryGetValue(HealthCheckName, out HealthReportEntry entry));
        Assert.Equal(HealthStatus.Healthy, entry.Status);
    }
}
