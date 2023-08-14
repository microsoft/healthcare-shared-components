// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Keys;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.CustomerManagedKey.Configs;

namespace Microsoft.Health.CustomerManagedKey.Health;
public class CustomerKeyValidationBackgroundService : BackgroundService
{
    private const string AccessLostMessage = "Access to the customer-managed key has been lost";

    private readonly ICustomerManagedKeyStatus _customerManagedKeyStatus;

    private readonly KeyClient _keyClient;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;
    private readonly IKeyTestProvider _keyTestProvider;
    private readonly ILogger<CustomerKeyValidationBackgroundService> _logger;

    public CustomerKeyValidationBackgroundService(
        KeyClient keyClient,
        IOptions<CustomerManagedKeyOptions> customerManagedKeyOptions,
        IKeyTestProvider keyTestProvider,
        ICustomerManagedKeyStatus customerManagedKeyStatus,
        ILogger<CustomerKeyValidationBackgroundService> logger)
    {
        EnsureArg.IsNotNull(customerManagedKeyOptions, nameof(customerManagedKeyOptions));

        _keyClient = EnsureArg.IsNotNull(keyClient, nameof(keyClient));
        _customerManagedKeyOptions = customerManagedKeyOptions.Value;
        _keyTestProvider = EnsureArg.IsNotNull(keyTestProvider, nameof(keyTestProvider));
        _customerManagedKeyStatus = EnsureArg.IsNotNull(customerManagedKeyStatus, nameof(customerManagedKeyStatus));
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
            await _keyTestProvider.PerformTestAsync(_keyClient, _customerManagedKeyOptions, cancellationToken).ConfigureAwait(false);

            _customerManagedKeyStatus.ExternalResourceHealth = new ExternalResourceHealth
            {
                IsHealthy = true,
                Description = null,
                Reason = ExternalHealthReason.None,
                Exception = null,
            };
        }
        catch (Exception ex) when (ex is RequestFailedException || ex is CryptographicException || ex is InvalidOperationException || ex is NotSupportedException)
        {
            _logger.LogInformation(ex, AccessLostMessage);

            _customerManagedKeyStatus.ExternalResourceHealth = new ExternalResourceHealth
            {
                IsHealthy = false,
                Description = AccessLostMessage,
                Reason = ExternalHealthReason.CustomerManagedKeyAccessLost,
                Exception = ex,
            };
        }
    }
}
