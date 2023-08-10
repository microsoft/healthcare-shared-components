// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.CustomerManagedKey.Configs;

namespace Microsoft.Health.CustomerManagedKey.Health;
public class EncryptionHealthCheck : IHealthCheck
{
    private const string AccessLostMessage = "Access to the customer-managed key has been lost";

    private readonly KeyClient _keyClient;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;
    private readonly IKeyTestProvider _keyTestProvider;

    private readonly ILogger<EncryptionHealthCheck> _logger;

    public EncryptionHealthCheck(
        KeyClient keyClient,
        IOptions<CustomerManagedKeyOptions> cmkOptions,
        IKeyTestProvider keyTestProvider,
        ILogger<EncryptionHealthCheck> logger)
    {
        EnsureArg.IsNotNull(cmkOptions, nameof(cmkOptions));
        EnsureArg.IsNotNull(keyTestProvider, nameof(keyTestProvider));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _keyClient = keyClient;
        _customerManagedKeyOptions = cmkOptions.Value;
        _keyTestProvider = keyTestProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing health check.");

        try
        {
            await _keyTestProvider.PerformTestAsync(_keyClient, _customerManagedKeyOptions, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Successfully connected.");
        }
        catch (Exception ex) when (ex is RequestFailedException || ex is CryptographicException || ex is InvalidOperationException || ex is NotSupportedException)
        {
            _logger.LogInformation(ex, AccessLostMessage);

            return HealthCheckResult.Degraded(
                AccessLostMessage,
                exception: ex,
                new Dictionary<string, object> { { DegradedHealthStatusData.CustomerManagedKeyAccessLost.ToString(), true } });
        }
    }
}
