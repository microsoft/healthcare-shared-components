// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Health;

namespace Microsoft.Health.Encryption.Customer.Extensions;

public static class CustomerKeyRegistrationExtensions
{
    public static IServiceCollection AddCustomerKeyValidationBackgroundService(this IServiceCollection services, Action<IOptions<CustomerManagedKeyOptions>> configure = null)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.AddSingleton<AsyncData<CustomerKeyHealth>>();
        services.AddHostedService<CustomerKeyValidationBackgroundService>();

        services.Configure(configure);
        services.TryAddSingleton<IExternalCredentialProvider, DefaultExternalCredentialProvider>();
        services.AddSingleton<IKeyTestProvider, KeyWrapUnwrapTestProvider>();

        return services;
    }
}
