// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Health;

namespace Microsoft.Health.Blob.Features.Health;

/// <summary>
/// Performs health checks on blob storage.
/// </summary>
public class BlobHealthCheck : IHealthCheck
{
    // This health check will not pass if the cached health is failing for any of these reasons
    private readonly IEnumerable<HealthStatusReason> _dependentHealthStatusReasons = new List<HealthStatusReason> { HealthStatusReason.CustomerManagedKeyAccessLost };
    private const string DegradedDescription = "The health of the store has degraded.";

    private readonly BlobServiceClient _client;
    private readonly BlobContainerConfiguration _blobContainerConfiguration;
    private readonly IBlobClientTestProvider _testProvider;
    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache;
    private readonly ILogger<BlobHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The cloud blob client factory.</param>
    /// <param name="namedBlobContainerConfigurationAccessor">The IOptions accessor to get a named container configuration version.</param>
    /// <param name="containerConfigurationName">Name to get corresponding container configuration.</param>
    /// <param name="testProvider">The blob test provider.</param>
    /// <param name="customerKeyHealthCache">The status of the customer-managed key</param>
    /// <param name="logger">The logger.</param>
    public BlobHealthCheck(
        BlobServiceClient client,
        IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        string containerConfigurationName,
        IBlobClientTestProvider testProvider,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        ILogger<BlobHealthCheck> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNullOrWhiteSpace(containerConfigurationName, nameof(containerConfigurationName));
        EnsureArg.IsNotNull(testProvider, nameof(testProvider));
        EnsureArg.IsNotNull(customerKeyHealthCache, nameof(customerKeyHealthCache));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _client = client;
        _blobContainerConfiguration = namedBlobContainerConfigurationAccessor.Get(containerConfigurationName);
        _testProvider = testProvider;
        _customerKeyHealthCache = customerKeyHealthCache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing health check.");

        CustomerKeyHealth cmkStatus = await _customerKeyHealthCache.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!cmkStatus.IsHealthy &&
            _dependentHealthStatusReasons.Contains(cmkStatus.Reason))
        {
            // if the customer-managed key is inaccessible, storage will also be inaccessible
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                cmkStatus.Exception,
                new Dictionary<string, object> { { "Reason", cmkStatus.Reason } });
        }

        await _testProvider.PerformTestAsync(_client, _blobContainerConfiguration, cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
