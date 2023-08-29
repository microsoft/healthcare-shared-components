// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Features.Health;

internal class HealthCheckCachePublisher : IHealthCheckPublisher
{
    private readonly ValueCache<HealthReport> _healthCheckReportCache;

    public HealthCheckCachePublisher(ValueCache<HealthReport> healthCheckReportCache)
    {
        _healthCheckReportCache = EnsureArg.IsNotNull(healthCheckReportCache, nameof(healthCheckReportCache));
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(report, nameof(report));

        _healthCheckReportCache.Set(report);

        return Task.CompletedTask;
    }
}
