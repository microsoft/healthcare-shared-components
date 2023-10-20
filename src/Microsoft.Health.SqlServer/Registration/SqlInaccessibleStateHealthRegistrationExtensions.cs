// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Extensions;
using Microsoft.Health.Encryption.Customer.Health;
using Microsoft.Health.SqlServer.Features.Health;

namespace Microsoft.Health.SqlServer.Registration;

public static class SqlInaccessibleStateHealthRegistrationExtensions
{
    public static IServiceCollection AddCustomerKeyAndSQLStateValidationBackgroundService(this IServiceCollection services, Action<CustomerManagedKeyOptions> configure = null)
    {
        services.AddCustomerKeyValidationBackgroundService(configure);
        services.AddSingleton<ICustomerKeyTestProvider, SQLInaccessibleStateTestProvider>();

        return services;
    }
}
