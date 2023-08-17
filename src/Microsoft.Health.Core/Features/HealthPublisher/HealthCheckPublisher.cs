// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Features.Health;

public class HealthCheckPublisher : IHealthCheckPublisher
{
    private readonly IHealthCheckReportCache _healthCheckReportCache;

    public HealthCheckPublisher(IHealthCheckReportCache healthCheckReportCache)
    {
        _healthCheckReportCache = EnsureArg.IsNotNull(healthCheckReportCache, nameof(healthCheckReportCache));
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(report, nameof(report));

        _healthCheckReportCache.SetCachedData(ToHealthCheckReport(report));

        return Task.CompletedTask;
    }

    private static HealthCheckReport ToHealthCheckReport(HealthReport healthReport)
    {
        return new HealthCheckReport
        {
            OverallStatus = healthReport.Status.ToString(),
            Details = healthReport.Entries.Select(entry => new HealthCheckReportEntry
            {
                Name = entry.Key,
                Status = Enum.GetName(typeof(HealthStatus), entry.Value.Status),
                Description = entry.Value.Description,
                Data = entry.Value.Data,
            }),
        };
    }
}
