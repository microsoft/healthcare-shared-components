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
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Configs;

namespace Microsoft.Health.Encryption.Customer.Health;

internal class CustomerKeyValidationBackgroundService : BackgroundService
{
    private const string AccessLostMessage = "Access to the customer-managed key has been lost";
    private const int DefaultKeyValidationPeriodInSeconds = 60;

    private readonly IKeyTestProvider _keyTestProvider;
    private readonly ValueCache<CustomerKeyHealth> _customerManagedKeyHealth;
    private readonly ILogger<CustomerKeyValidationBackgroundService> _logger;

    private readonly TimeSpan _keyValidationPeriod;

    public CustomerKeyValidationBackgroundService(
        IKeyTestProvider keyTestProvider,
        ValueCache<CustomerKeyHealth> customerManagedKeyHealth,
        IOptions<CustomerManagedKeyOptions> customerManagedKeyOptions,
        ILogger<CustomerKeyValidationBackgroundService> logger)
    {
        EnsureArg.IsNotNull(customerManagedKeyOptions, nameof(customerManagedKeyOptions));

        _keyTestProvider = EnsureArg.IsNotNull(keyTestProvider, nameof(keyTestProvider));
        _customerManagedKeyHealth = EnsureArg.IsNotNull(customerManagedKeyHealth, nameof(customerManagedKeyHealth));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        _keyValidationPeriod = customerManagedKeyOptions.Value != null && customerManagedKeyOptions.Value.KeyValidationPeriodInSeconds > 0 ?
            TimeSpan.FromSeconds(customerManagedKeyOptions.Value.KeyValidationPeriodInSeconds) :
            TimeSpan.FromSeconds(DefaultKeyValidationPeriodInSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckHealth(stoppingToken).ConfigureAwait(false);
                await Task.Delay(_keyValidationPeriod, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                _logger.LogInformation(e, $"{nameof(CustomerKeyValidationBackgroundService)} cancelled");
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This background service failing should not crash the application.")]
    internal async Task CheckHealth(CancellationToken cancellationToken)
    {
        try
        {
            await _keyTestProvider.AssertHealthAsync(cancellationToken).ConfigureAwait(false);
            SetHealthy();
        }
        catch (Exception ex) when (ex is RequestFailedException || ex is CryptographicException || ex is InvalidOperationException || ex is NotSupportedException)
        {
            _logger.LogInformation(ex, AccessLostMessage);
            SetUnhealthy(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(CustomerKeyValidationBackgroundService)} has failed unexpectedly.");

            // reset to healthy so unexpected errors are not categorized as a customer misconfiguration
            SetHealthy();
        }
    }

    private void SetUnhealthy(Exception ex)
    {
        _customerManagedKeyHealth.SetCachedData(new CustomerKeyHealth
        {
            IsHealthy = false,
            Reason = ExternalHealthReason.CustomerManagedKeyAccessLost,
            Exception = ex,
        });
    }

    private void SetHealthy()
    {
        _customerManagedKeyHealth.SetCachedData(new CustomerKeyHealth
        {
            IsHealthy = true,
            Reason = ExternalHealthReason.None,
            Exception = null,
        });
    }
}
