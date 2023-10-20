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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Configs;

namespace Microsoft.Health.Encryption.Customer.Health;

internal class CustomerKeyValidationBackgroundService : BackgroundService
{
    private readonly IOrderedEnumerable<ICustomerKeyTestProvider> _customerKeyTestProviders;
    private readonly ValueCache<CustomerKeyHealth> _customerManagedKeyHealth;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;
    private readonly ILogger<CustomerKeyValidationBackgroundService> _logger;

    public CustomerKeyValidationBackgroundService(
        IEnumerable<ICustomerKeyTestProvider> keyTestProviders,
        ValueCache<CustomerKeyHealth> customerManagedKeyHealth,
        IOptions<CustomerManagedKeyOptions> customerManagedKeyOptions,
        ILogger<CustomerKeyValidationBackgroundService> logger)
    {
        EnsureArg.IsNotNull(customerManagedKeyOptions, nameof(customerManagedKeyOptions));
        EnsureArg.IsNotNull(keyTestProviders, nameof(keyTestProviders));
        EnsureArg.IsTrue(keyTestProviders.Any(), nameof(keyTestProviders));

        _customerKeyTestProviders = keyTestProviders.OrderBy(k => k.Priority);
        _customerManagedKeyHealth = EnsureArg.IsNotNull(customerManagedKeyHealth, nameof(customerManagedKeyHealth));
        _customerManagedKeyOptions = EnsureArg.IsNotNull(customerManagedKeyOptions.Value, nameof(customerManagedKeyOptions.Value));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckHealth(stoppingToken).ConfigureAwait(false);
                await Task.Delay(_customerManagedKeyOptions.KeyValidationPeriod, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                _logger.LogInformation(e, $"{nameof(CustomerKeyValidationBackgroundService)} cancelled");
            }
        }
    }

    internal async Task CheckHealth(CancellationToken cancellationToken)
    {
        foreach (ICustomerKeyTestProvider customerKeyTestProvider in _customerKeyTestProviders)
        {
            CustomerKeyHealth health = await customerKeyTestProvider.AssertHealthAsync(cancellationToken).ConfigureAwait(false);

            if (!health.IsHealthy)
            {
                _customerManagedKeyHealth.Set(health);
                return;
            }
        }

        _customerManagedKeyHealth.Set(new CustomerKeyHealth());
    }
}
