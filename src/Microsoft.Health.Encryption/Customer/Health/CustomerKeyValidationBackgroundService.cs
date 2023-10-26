// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Configs;

namespace Microsoft.Health.Encryption.Customer.Health;

internal class CustomerKeyValidationBackgroundService : BackgroundService
{
    private readonly IKeyWrapUnwrapTestProvider _keyWrapUnwrapTestProvider;
    private readonly ValueCache<CustomerKeyHealth> _customerManagedKeyHealth;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;

    public CustomerKeyValidationBackgroundService(
        IKeyWrapUnwrapTestProvider keyTestProvider,
        ValueCache<CustomerKeyHealth> customerManagedKeyHealth,
        IOptions<CustomerManagedKeyOptions> customerManagedKeyOptions)
    {
        EnsureArg.IsNotNull(customerManagedKeyOptions, nameof(customerManagedKeyOptions));
        EnsureArg.IsNotNull(keyTestProvider, nameof(keyTestProvider));

        _keyWrapUnwrapTestProvider = keyTestProvider;
        _customerManagedKeyHealth = EnsureArg.IsNotNull(customerManagedKeyHealth, nameof(customerManagedKeyHealth));
        _customerManagedKeyOptions = EnsureArg.IsNotNull(customerManagedKeyOptions.Value, nameof(customerManagedKeyOptions.Value));
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task CheckHealth(CancellationToken cancellationToken)
    {
        CustomerKeyHealth customerKeyHealth = await _keyWrapUnwrapTestProvider.AssertHealthAsync(cancellationToken).ConfigureAwait(false);
        _customerManagedKeyHealth.Set(customerKeyHealth);
    }
}
