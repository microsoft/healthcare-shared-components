// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Features.Health;

public static class HealthCheckPublisherExtensions
{
    public static IServiceCollection AddHealthCheckPublisher(this IServiceCollection services, Action<HealthCheckPublisherOptions> configure = null)
    {
        EnsureArg.IsNotNull(configure, nameof(configure));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthCheckPublisher, HealthCheckCachePublisher>());
        services.AddSingleton<ValueCache<HealthReport>>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
