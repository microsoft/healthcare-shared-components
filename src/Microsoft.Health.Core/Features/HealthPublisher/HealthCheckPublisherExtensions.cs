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
            options.Period = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<IHealthCheckPublisher, HealthCheckPublisher>();
        services.AddSingleton<AsyncData<HealthReport>>();

        return services;
    }
}
