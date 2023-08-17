// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Encryption.Configs;
using Microsoft.Health.Encryption.Health;

namespace Microsoft.Health.Encryption.Extensions;

public static class CustomerKeyRegistrationExtensions
{
    public static IServiceCollection AddCustomerKeyValidationBackgroundService(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.AddCustomerKeyHealthTest();
        services.AddSingleton<AsyncData<CustomerKeyHealth>>();
        services.AddHostedService<CustomerKeyValidationBackgroundService>();

        return services;
    }

    public static IServiceCollection AddCustomerKeyHealthTest(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.AddOptions<CustomerManagedKeyOptions>();
        services.TryAddSingleton<IExternalCredentialProvider, DefaultExternalCredentialProvider>();
        services.TryAddSingleton<IKeyTestProvider, KeyWrapUnwrapTestProvider>();

        return services;
    }
}
