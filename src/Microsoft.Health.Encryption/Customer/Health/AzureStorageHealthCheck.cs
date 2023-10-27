// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using System.Threading;
using Azure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;
using System.Collections.Generic;

namespace Microsoft.Health.Encryption.Customer.Health;

public abstract class AzureStorageHealthCheck : StorageHealthCheck
{
    internal AzureStorageHealthCheck(ValueCache<CustomerKeyHealth> customerKeyHealthCache, ILogger<StorageHealthCheck> logger)
        : base(customerKeyHealthCache, logger)
    {
    }

    public override async Task<HealthCheckResult> CheckStorageHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await CheckAzureStorageHealthAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is RequestFailedException rfe && rfe.ErrorCode == "KeyVaultEncryptionKeyNotFound")
        {
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                ex,
                new Dictionary<string, object> { { "Reason", HealthStatusReason.CustomerManagedKeyAccessLost } });
        }
    }

    public abstract Task<HealthCheckResult> CheckAzureStorageHealthAsync(CancellationToken cancellationToken);
}
