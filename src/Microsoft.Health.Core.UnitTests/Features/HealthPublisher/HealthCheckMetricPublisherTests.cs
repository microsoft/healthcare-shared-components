// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.HealthPublisher;
using Microsoft.Health.Core.Features.Metric;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Core.UnitTests.Features.HealthPublisher;

public class HealthCheckMetricPublisherTests
{
    private readonly IResourceHealthSignalProvider _resourceHealthSignalProvider = Substitute.For<IResourceHealthSignalProvider>();
    private readonly IOptions<ResourceHealthDimensionOptions> _resourceHealthDimensionOptions = Substitute.For<IOptions<ResourceHealthDimensionOptions>>();

    private readonly ResourceHealthDimensionOptions _resourceHealthDimensions;

    private readonly HealthCheckMetricPublisher _healthCheckMetricPublisher;

    public HealthCheckMetricPublisherTests()
    {
        _resourceHealthDimensions = new ResourceHealthDimensionOptions()
        {
            ArmGeoLocation = "westus2",
            ArmResourceId = "someResourceId"
        };

        _resourceHealthDimensionOptions.Value.Returns(_resourceHealthDimensions);
        _healthCheckMetricPublisher = new HealthCheckMetricPublisher(_resourceHealthSignalProvider, _resourceHealthDimensionOptions, new NullLogger<HealthCheckMetricPublisher>());
    }

    [Fact]
    public void GivenReportWithServiceUnavailable_PublishAsync_ServiceUnavailableIsPublished()
    {
        HealthReport report = CreateDummyHealthReport(
            HealthStatus.Unhealthy,
            new HealthStatusReason[] {
                HealthStatusReason.ServiceUnavailable,
                HealthStatusReason.CustomerManagedKeyAccessLost,
                HealthStatusReason.None });

        _healthCheckMetricPublisher.PublishAsync(report, CancellationToken.None);

        _resourceHealthSignalProvider.Received(1).EmitHealthMetric(HealthStatusReason.ServiceUnavailable, _resourceHealthDimensions);
    }

    [Fact]
    public void GivenReportWithCMKAccessLost_PublishAsync_CMKAccessLostIsPublished()
    {
        HealthReport report = CreateDummyHealthReport(
            HealthStatus.Degraded,
            new HealthStatusReason[] {
                HealthStatusReason.ServiceDegraded,
                HealthStatusReason.CustomerManagedKeyAccessLost,
                HealthStatusReason.None });

        _healthCheckMetricPublisher.PublishAsync(report, CancellationToken.None);

        _resourceHealthSignalProvider.Received(1).EmitHealthMetric(HealthStatusReason.CustomerManagedKeyAccessLost, _resourceHealthDimensions);
    }

    [Fact]
    public void GivenReportWithServiceDegraded_PublishAsync_ServiceDegradedIsPublished()
    {
        HealthReport report = CreateDummyHealthReport(
            HealthStatus.Degraded,
            new HealthStatusReason[] {
                HealthStatusReason.ServiceDegraded,
                HealthStatusReason.None });

        _healthCheckMetricPublisher.PublishAsync(report, CancellationToken.None);

        _resourceHealthSignalProvider.Received(1).EmitHealthMetric(HealthStatusReason.ServiceDegraded, _resourceHealthDimensions);
    }

    [Fact]
    public void GivenReportWithNone_PublishAsync_NoneIsPublished()
    {
        HealthReport report = CreateDummyHealthReport(
            HealthStatus.Healthy,
            new HealthStatusReason[] {
                HealthStatusReason.None,
                HealthStatusReason.None });

        _healthCheckMetricPublisher.PublishAsync(report, CancellationToken.None);

        _resourceHealthSignalProvider.Received(1).EmitHealthMetric(HealthStatusReason.None, _resourceHealthDimensions);
    }

    [Fact]
    public void GivenDegradedReportWithNoneReason_PublishAsync_ServiceDegradedIsPublished()
    {
        HealthReport report = CreateDummyHealthReport(
            HealthStatus.Degraded,
            new HealthStatusReason[] {
                HealthStatusReason.None,
                HealthStatusReason.None });

        _healthCheckMetricPublisher.PublishAsync(report, CancellationToken.None);

        _resourceHealthSignalProvider.Received(1).EmitHealthMetric(HealthStatusReason.ServiceDegraded, _resourceHealthDimensions);
    }

    [Fact]
    public void GivenUnhealthyReportWithNoneReason_PublishAsync_ServiceUnavailableIsPublished()
    {
        HealthReport report = CreateDummyHealthReport(
            HealthStatus.Unhealthy,
            new HealthStatusReason[] {
                HealthStatusReason.None,
                HealthStatusReason.None });

        _healthCheckMetricPublisher.PublishAsync(report, CancellationToken.None);

        _resourceHealthSignalProvider.Received(1).EmitHealthMetric(HealthStatusReason.ServiceUnavailable, _resourceHealthDimensions);
    }

    private static HealthReport CreateDummyHealthReport(HealthStatus overallStatus, HealthStatusReason[] includedStatusAndReasons)
    {
        Dictionary<string, HealthReportEntry> entries = new Dictionary<string, HealthReportEntry>();

        foreach (var includedReason in includedStatusAndReasons)
        {
            entries.Add($"someEntry{entries.Count}", new HealthReportEntry(
                HealthStatus.Healthy,
                "some description",
                TimeSpan.Zero,
                exception: null,
                data: new Dictionary<string, object>() { { "Reason", includedReason } }));
        }

        return new HealthReport(entries, overallStatus, TimeSpan.Zero);
    }
}
