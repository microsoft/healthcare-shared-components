// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Metric;

namespace Microsoft.Health.Core.Features.HealthPublisher;
internal class HealthCheckMetricPublisher : IHealthCheckPublisher
{
    private readonly IResourceHealthSignalProvider _resourceHealthMeter;
    private readonly ResourceHealthDimensionOptions _resourceHealthDimensionOptions;

    public HealthCheckMetricPublisher(IResourceHealthSignalProvider resourceHealthMeter, IOptions<ResourceHealthDimensionOptions> resourceHealthDimensionOptions)
    {
        EnsureArg.IsNotNull(resourceHealthDimensionOptions, nameof(resourceHealthDimensionOptions));

        _resourceHealthDimensionOptions = EnsureArg.IsNotNull(resourceHealthDimensionOptions.Value, nameof(resourceHealthDimensionOptions.Value));
        _resourceHealthMeter = EnsureArg.IsNotNull(resourceHealthMeter, nameof(resourceHealthMeter));

        EnsureArg.IsNotNull(_resourceHealthDimensionOptions.ArmGeoLocation, nameof(_resourceHealthDimensionOptions.ArmGeoLocation));
        EnsureArg.IsNotNull(_resourceHealthDimensionOptions.ResourceType, nameof(_resourceHealthDimensionOptions.ResourceType));
        EnsureArg.IsNotNull(_resourceHealthDimensionOptions.ArmResourceId, nameof(_resourceHealthDimensionOptions.ArmResourceId));
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(report, nameof(report));

        switch (report.Status)
        {
            case HealthStatus.Healthy:
                _resourceHealthMeter.EmitHealthMetric(HealthStatusReason.None, _resourceHealthDimensionOptions);
                break;
            case HealthStatus.Degraded:
                HealthStatusReason reason = report.GetHighestSeverityReason(defaultReason: HealthStatusReason.ServiceDegraded);
                _resourceHealthMeter.EmitHealthMetric(reason, _resourceHealthDimensionOptions);
                break;
            case HealthStatus.Unhealthy:
            default:
                _resourceHealthMeter.EmitHealthMetric(HealthStatusReason.ServiceUnavailable, _resourceHealthDimensionOptions);
                break;
        }

        return Task.CompletedTask;
    }
}
