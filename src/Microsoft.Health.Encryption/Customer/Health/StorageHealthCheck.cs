// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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

    internal StorageHealthCheck(ValueCache<CustomerKeyHealth> customerKeyHealthCache, ILogger<StorageHealthCheck> logger)
    {
        _customerKeyHealthCache = EnsureArg.IsNotNull(customerKeyHealthCache, nameof(customerKeyHealthCache));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking customer key health");

        CustomerKeyHealth cmkStatus = await _customerKeyHealthCache.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!cmkStatus.IsHealthy && DependentHealthReasons.Contains(cmkStatus.Reason))
        {
            // if the customer-managed key is inaccessible, storage will also be inaccessible
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                cmkStatus.Exception,
                new Dictionary<string, object> { { "Reason", cmkStatus.Reason } });
        }

        try
        {
            await CheckStorageHealthAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Successfully connected.");
        }
        catch (Exception ex) when (CMKAccessLostExceptionFilter(ex))
        {
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                ex,
                new Dictionary<string, object> { { "Reason", HealthStatusReason.CustomerManagedKeyAccessLost } });
        }
    }

    public abstract string DegradedDescription { get; }

    public abstract IEnumerable<HealthStatusReason> DependentHealthReasons { get; }

    public abstract Func<Exception, bool> CMKAccessLostExceptionFilter { get; }

    public abstract Task CheckStorageHealthAsync(CancellationToken cancellationToken);
}
