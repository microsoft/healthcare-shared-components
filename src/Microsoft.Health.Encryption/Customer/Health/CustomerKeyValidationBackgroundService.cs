// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Configs;

namespace Microsoft.Health.Encryption.Customer.Health;

internal class CustomerKeyValidationBackgroundService : BackgroundService
{
    private readonly IKeyTestProvider _keyWrapUnwrapTestProvider;
    private readonly ValueCache<CustomerKeyHealth> _customerManagedKeyHealth;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;
    private readonly ILogger<CustomerKeyValidationBackgroundService> _logger;

    public CustomerKeyValidationBackgroundService(
        IKeyTestProvider keyTestProvider,
        ValueCache<CustomerKeyHealth> customerManagedKeyHealth,
        IOptions<CustomerManagedKeyOptions> customerManagedKeyOptions,
        ILogger<CustomerKeyValidationBackgroundService> logger)
    {
        EnsureArg.IsNotNull(customerManagedKeyOptions, nameof(customerManagedKeyOptions));
        EnsureArg.IsNotNull(keyTestProvider, nameof(keyTestProvider));

        _keyWrapUnwrapTestProvider = keyTestProvider;
        _customerManagedKeyHealth = EnsureArg.IsNotNull(customerManagedKeyHealth, nameof(customerManagedKeyHealth));
        _customerManagedKeyOptions = EnsureArg.IsNotNull(customerManagedKeyOptions.Value, nameof(customerManagedKeyOptions.Value));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckHealth(stoppingToken).ConfigureAwait(false);
            await Task.Delay(_customerManagedKeyOptions.KeyValidationPeriod, stoppingToken).ConfigureAwait(false);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This background service failing should not crash the application.")]
    internal async Task CheckHealth(CancellationToken cancellationToken)
    {
        try
        {
            CustomerKeyHealth customerKeyHealth = await _keyWrapUnwrapTestProvider.AssertHealthAsync(cancellationToken).ConfigureAwait(false);
            _customerManagedKeyHealth.Set(customerKeyHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(CustomerKeyValidationBackgroundService)} has failed unexpectedly.");

            // reset to healthy so unexpected errors are not categorized as a customer misconfiguration
            _customerManagedKeyHealth.Set(new CustomerKeyHealth());
        }
    }
}
