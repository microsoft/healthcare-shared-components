// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Metric;

namespace Microsoft.Health.Core.Features.HealthPublisher;
internal class HealthCheckMetricPublisher : IHealthCheckPublisher
{
    public static IReadOnlyDictionary<HealthStatus, HealthStatusReason> DefaultStatusToReasonMapping = new Dictionary<HealthStatus, HealthStatusReason>()
    {
        { HealthStatus.Healthy, HealthStatusReason.None },
        { HealthStatus.Degraded, HealthStatusReason.ServiceDegraded },
        { HealthStatus.Unhealthy, HealthStatusReason.ServiceUnavailable },
    };

    private readonly IResourceHealthSignalProvider _resourceHealthSignalProvider;
    private readonly ResourceHealthDimensionOptions _resourceHealthDimensionOptions;
    private readonly ILogger<HealthCheckMetricPublisher> _logger;

    public HealthCheckMetricPublisher(IResourceHealthSignalProvider resourceHealthSignalProvider, IOptions<ResourceHealthDimensionOptions> resourceHealthDimensionOptions, ILogger<HealthCheckMetricPublisher> logger)
    {
        EnsureArg.IsNotNull(resourceHealthDimensionOptions, nameof(resourceHealthDimensionOptions));

        _resourceHealthDimensionOptions = EnsureArg.IsNotNull(resourceHealthDimensionOptions.Value, nameof(resourceHealthDimensionOptions.Value));
        _resourceHealthSignalProvider = EnsureArg.IsNotNull(resourceHealthSignalProvider, nameof(resourceHealthSignalProvider));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(report, nameof(report));

        HealthStatusReason defaultReason = GetDefaultReasonForStatus(report);

        switch (report.Status)
        {
            case HealthStatus.Healthy:
                _resourceHealthSignalProvider.EmitHealthMetric(defaultReason, _resourceHealthDimensionOptions);
                break;
            case HealthStatus.Degraded:
                HealthStatusReason reason = report.GetHighestSeverityReason(defaultReason: defaultReason);
                _resourceHealthSignalProvider.EmitHealthMetric(reason, _resourceHealthDimensionOptions);
                break;
            case HealthStatus.Unhealthy:
            default:
                _resourceHealthSignalProvider.EmitHealthMetric(defaultReason, _resourceHealthDimensionOptions);
                break;
        }

        return Task.CompletedTask;
    }

    private HealthStatusReason GetDefaultReasonForStatus(HealthReport report)
    {
        if (DefaultStatusToReasonMapping.TryGetValue(report.Status, out HealthStatusReason defaultReason))
        {
            return defaultReason;
        }

        _logger.LogError("No default HealthStatusReason can be found for HealthStatus {HealthStatus}", report.Status);
        return HealthStatusReason.None;
    }
}
