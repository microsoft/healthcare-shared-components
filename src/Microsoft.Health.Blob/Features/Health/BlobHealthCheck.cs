// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.CustomerManagedKey.Configs;
using Microsoft.Health.CustomerManagedKey.Health;

namespace Microsoft.Health.Blob.Features.Health;

/// <summary>
/// Performs health checks on blob storage.
/// </summary>
public class BlobHealthCheck : IHealthCheck
{
    private const string AccessLostMessage = "Access to the customer-managed key has been lost";

    private readonly KeyClient _keyClient;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;
    private readonly IKeyTestProvider _keyTestProvider;

    private readonly BlobServiceClient _client;
    private readonly BlobContainerConfiguration _blobContainerConfiguration;
    private readonly IBlobClientTestProvider _testProvider;
    private readonly ILogger<BlobHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobHealthCheck"/> class.
    /// </summary>
    /// <param name="keyClient">The key vault client</param>
    /// <param name="client">The cloud blob client factory.</param>
    /// <param name="namedBlobContainerConfigurationAccessor">The IOptions accessor to get a named container configuration version.</param>
    /// <param name="cmkOptions">The IOptions for customer managed keys configuration</param>
    /// <param name="containerConfigurationName">Name to get corresponding container configuration.</param>
    /// <param name="keyTestProvider">The key test provider</param>
    /// <param name="testProvider">The blob test provider.</param>
    /// <param name="logger">The logger.</param>
    public BlobHealthCheck(
        KeyClient keyClient,
        BlobServiceClient client,
        IOptions<CustomerManagedKeyOptions> cmkOptions,
        IOptionsSnapshot<BlobContainerConfiguration> namedBlobContainerConfigurationAccessor,
        string containerConfigurationName,
        IKeyTestProvider keyTestProvider,
        IBlobClientTestProvider testProvider,
        ILogger<BlobHealthCheck> logger)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(cmkOptions, nameof(cmkOptions));
        EnsureArg.IsNotNull(namedBlobContainerConfigurationAccessor, nameof(namedBlobContainerConfigurationAccessor));
        EnsureArg.IsNotNullOrWhiteSpace(containerConfigurationName, nameof(containerConfigurationName));
        EnsureArg.IsNotNull(keyTestProvider, nameof(keyTestProvider));
        EnsureArg.IsNotNull(testProvider, nameof(testProvider));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _keyClient = keyClient;
        _client = client;
        _customerManagedKeyOptions = cmkOptions.Value;
        _blobContainerConfiguration = namedBlobContainerConfigurationAccessor.Get(containerConfigurationName);
        _keyTestProvider = keyTestProvider;
        _testProvider = testProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing health check.");

        try
        {
            await _keyTestProvider.PerformTestAsync(_keyClient, _customerManagedKeyOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException requestFailedException)
        {
            _logger.LogInformation(requestFailedException, AccessLostMessage);

            return HealthCheckResult.Degraded(
                AccessLostMessage,
                exception: requestFailedException,
                new Dictionary<string, object> { { DegradedHealthStatusData.CustomerManagedKeyAccessLost.ToString(), true } });
        }

        await _testProvider.PerformTestAsync(_client, _blobContainerConfiguration, cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
