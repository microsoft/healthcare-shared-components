// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Features.Health;

public static class HealthCheckPublisherExtensions
{
    public static IServiceCollection AddHealthCheckPublisher(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.Configure<HealthCheckPublisherOptions>(options =>
        {
            // 20 second start-up delay to give prerequisite health checks (ex: customer-managed keys) time to initialize
            options.Delay = TimeSpan.FromSeconds(20);
            options.Period = TimeSpan.FromSeconds(15);
        });
        services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();

        return services;
    }
}
