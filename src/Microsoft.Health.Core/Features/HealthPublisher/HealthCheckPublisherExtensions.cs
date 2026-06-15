// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.HealthPublisher;
using Microsoft.Health.Core.Features.Metric;

namespace Microsoft.Health.Core.Features.Health;

public static class HealthCheckPublisherExtensions
{
    public static IServiceCollection AddHealthCheckCachePublisher(this IServiceCollection services, Action<HealthCheckPublisherOptions> configure = null, Action<HealthReportCachingOptions> configureReportCache = null)
    {
        EnsureArg.IsNotNull(configure, nameof(configure));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthCheckPublisher, HealthCheckCachePublisher>());
        services.AddSingleton<ValueCache<HealthReport>>(sp =>
        {
            HealthReportCachingOptions reportCacheOptions = sp.GetService<IOptions<HealthReportCachingOptions>>()?.Value ?? new HealthReportCachingOptions();
            return new ValueCache<HealthReport>(reportCacheOptions.Expiry);
        });

        if (configure != null)
        {
            services.Configure(configure);
        }

        if (configureReportCache != null)
        {
            services.Configure(configureReportCache);
        }

        return services;
    }

    public static IServiceCollection AddHealthCheckMetricPublisher(this IServiceCollection services, Action<HealthCheckPublisherOptions> configure = null, Action<ResourceHealthDimensionOptions> configureDimensions = null)
    {
        EnsureArg.IsNotNull(configure, nameof(configure));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthCheckPublisher, HealthCheckMetricPublisher>());
        services.TryAddSingleton<IResourceHealthSignalProvider, DefaultResourceHealthSignalProvider>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        if (configureDimensions != null)
        {
            services.Configure(configureDimensions);
        }

        return services;
    }
}
