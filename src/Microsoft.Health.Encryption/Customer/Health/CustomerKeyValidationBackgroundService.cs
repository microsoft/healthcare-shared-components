// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Encryption.Customer.Health;

internal class CustomerKeyValidationBackgroundService : BackgroundService
{
    private const string AccessLostMessage = "Access to the customer-managed key has been lost";

    private readonly AsyncData<CustomerKeyHealth> _customerManagedKeyHealth;

    private readonly IKeyTestProvider _keyTestProvider;
    private readonly ILogger<CustomerKeyValidationBackgroundService> _logger;

    public CustomerKeyValidationBackgroundService(
        IKeyTestProvider keyTestProvider,
        AsyncData<CustomerKeyHealth> customerManagedKeyHealth,
        ILogger<CustomerKeyValidationBackgroundService> logger)
    {
        _keyTestProvider = EnsureArg.IsNotNull(keyTestProvider, nameof(keyTestProvider));
        _customerManagedKeyHealth = EnsureArg.IsNotNull(customerManagedKeyHealth, nameof(customerManagedKeyHealth));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckHealth(stoppingToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                _logger.LogInformation(e, $"{nameof(CustomerKeyValidationBackgroundService)} cancelled");
            }
        }
    }

    internal async Task CheckHealth(CancellationToken cancellationToken)
    {
        try
        {
            await _keyTestProvider.PerformTestAsync(cancellationToken).ConfigureAwait(false);

            _customerManagedKeyHealth.SetCachedData(new CustomerKeyHealth
            {
                IsHealthy = true,
                Description = null,
                Reason = ExternalHealthReason.None,
                Exception = null,
            });
        }
        catch (Exception ex) when (ex is RequestFailedException || ex is CryptographicException || ex is InvalidOperationException || ex is NotSupportedException)
        {
            _logger.LogInformation(ex, AccessLostMessage);

            _customerManagedKeyHealth.SetCachedData(new CustomerKeyHealth
            {
                IsHealthy = false,
                Description = AccessLostMessage,
                Reason = ExternalHealthReason.CustomerManagedKeyAccessLost,
                Exception = ex,
            });
        }
    }
}
