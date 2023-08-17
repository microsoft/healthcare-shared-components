// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
using Microsoft.Health.CustomerManagedKey.Health;

namespace Microsoft.Health.Blob.Features.Health;

/// <summary>
/// Performs health checks on blob storage.
/// </summary>
public class BlobHealthCheck : IHealthCheck
{
    private readonly ICustomerManagedKeyStatusCache _customerManagedKeyStatus;

    private readonly BlobServiceClient _client;
    private readonly BlobContainerConfiguration _blobContainerConfiguration;
    private readonly IBlobClientTestProvider _testProvider;
    private readonly ILogger<BlobHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobHealthCheck"/> class.
    /// </summary>
    /// <param name="client">The cloud blob client factory.</param>
    /// <param name="namedBlobContainerConfigurationAccessor">The IOptions accessor to get a named container configuration version.</param>
    /// <param name="containerConfigurationName">Name to get corresponding container configuration.</param>
    /// <param name="testProvider">The blob test provider.</param>
    /// <param name="customerManagedKeyStatus">The status of the customer-managed key</param>
    /// <param name="logger">The logger.</param>
    public BlobHealthCheck(
        BlobServiceClient client,
        IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        string containerConfigurationName,
        IBlobClientTestProvider testProvider,
        ICustomerManagedKeyStatusCache customerManagedKeyStatus,
        ILogger<BlobHealthCheck> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNullOrWhiteSpace(containerConfigurationName, nameof(containerConfigurationName));
        EnsureArg.IsNotNull(testProvider, nameof(testProvider));
        EnsureArg.IsNotNull(customerManagedKeyStatus, nameof(customerManagedKeyStatus));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _client = client;
        _blobContainerConfiguration = namedBlobContainerConfigurationAccessor.Get(containerConfigurationName);
        _testProvider = testProvider;
        _customerManagedKeyStatus = customerManagedKeyStatus;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing health check.");

        IExternalResourceHealth cmkStatus = await _customerManagedKeyStatus.GetCachedData().ConfigureAwait(false);
        if (!cmkStatus.IsHealthy)
        {
            // if the customer-managed key is inaccessible, storage will also be inaccessible
            return new HealthCheckResult(
                HealthStatus.Degraded,
                cmkStatus.Description,
                cmkStatus.Exception,
                new Dictionary<string, object> { { cmkStatus.Reason.ToString(), true } });
        }

        await _testProvider.PerformTestAsync(_client, _blobContainerConfiguration, cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
