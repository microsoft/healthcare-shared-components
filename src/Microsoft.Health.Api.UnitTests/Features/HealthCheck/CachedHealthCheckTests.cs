// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
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
        TestHealthCheck.Implementation.ClearReceivedCalls();
        TestHealthCheck.Implementation
            .CheckHealthAsync(default, default)
            .ReturnsForAnyArgs(HealthCheckResult.Healthy());

        ServiceCollection services = new();
        services
            .AddHealthChecks()
            .AddCheck<TestHealthCheck>(HealthCheckName);

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
    private readonly ServiceProvider _serviceProvider;

    [Fact]
    public async Task GivenCachedHealthCheck_WhenRunningHealthCheckService_ThenPreserveCacheBetweenInvocations()
    {
        HealthCheckService service = _serviceProvider.GetRequiredService<HealthCheckService>();

        using CancellationTokenSource cts = new();

        AssertHealthReport(await service.CheckHealthAsync(cts.Token));
        await TestHealthCheck.Implementation.ReceivedWithAnyArgs(1).CheckHealthAsync(default, default);

        // Attempt to concurrent invoke it multiple times
        HealthReport[] reports = await Task.WhenAll(
            service.CheckHealthAsync(cts.Token),
            service.CheckHealthAsync(cts.Token),
            service.CheckHealthAsync(cts.Token));

        Assert.All(reports, AssertHealthReport);
        await TestHealthCheck.Implementation.ReceivedWithAnyArgs(1).CheckHealthAsync(default, default);
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Type is registered in test service collection.")]
    private sealed class TestHealthCheck : IHealthCheck
    {
        public static readonly IHealthCheck Implementation = Substitute.For<IHealthCheck>();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Implementation.CheckHealthAsync(context, cancellationToken);
    }

    private static void AssertHealthReport(HealthReport actual)
    {
        Assert.Equal(HealthStatus.Healthy, actual.Status);
        Assert.True(actual.Entries.TryGetValue(HealthCheckName, out HealthReportEntry entry));
        Assert.Equal(HealthStatus.Healthy, entry.Status);
    }
}
