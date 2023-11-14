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
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Encryption.Customer.Health;

public abstract class StorageHealthCheck : IHealthCheck
{
    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache;
    private readonly ILogger<StorageHealthCheck> _logger;

    protected StorageHealthCheck(ValueCache<CustomerKeyHealth> customerKeyHealthCache, ILogger<StorageHealthCheck> logger)
    {
        _customerKeyHealthCache = EnsureArg.IsNotNull(customerKeyHealthCache, nameof(customerKeyHealthCache));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking customer key health");

        CustomerKeyHealth cmkStatus = await _customerKeyHealthCache.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!cmkStatus.IsHealthy)
        {
            // if the customer-managed key is inaccessible, storage will also be inaccessible
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                cmkStatus.Exception,
                new Dictionary<string, object> { { "Reason", cmkStatus.Reason.ToString() } });
        }

        return await CheckStorageHealthAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual string DegradedDescription => "The health of the store has degraded.";

    public abstract Task<HealthCheckResult> CheckStorageHealthAsync(CancellationToken cancellationToken);
}
