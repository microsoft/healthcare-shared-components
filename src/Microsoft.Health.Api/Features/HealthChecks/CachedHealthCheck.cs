// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Api.Features.HealthChecks;

internal sealed class CachedHealthCheck(IHealthCheck healthCheck, string name, HealthCheckCache cache) : IHealthCheck
{
    private readonly IHealthCheck _healthCheck = EnsureArg.IsNotNull(healthCheck, nameof(healthCheck));
    private readonly string _name = EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));
    private readonly HealthCheckCache _cache = EnsureArg.IsNotNull(cache, nameof(cache));

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => _cache.GetResultCache(_name).CheckHealthAsync(_healthCheck, context, cancellationToken);
}
