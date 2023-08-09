// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.CustomerManagedKey.Client;
using Microsoft.Health.CustomerManagedKey.Configs;

namespace Microsoft.Health.CustomerManagedKey.Extensions;
public static class CustomerKeyRegistrationExtensions
{
    public static IServiceCollection AddKeyClient(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.AddOptions<CustomerManagedKeyOptions>();

        services.TryAddSingleton<IExternalCredentialProvider, DefaultExternalCredentialProvider>();
        services.TryAddSingleton(p => CustomerKeyClientFactory.Create(
            p.GetRequiredService<IExternalCredentialProvider>(),
            p.GetRequiredService<IOptions<CustomerManagedKeyOptions>>().Value));
        return services;
    }
}
