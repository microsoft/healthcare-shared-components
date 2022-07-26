// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A collection of <see langword="static" /> methods for configuring Azure Functions services.
/// </summary>
public static class WebJobsServiceCollectionExtensions
{
    /// <inheritdoc cref="HealthCheckServiceCollectionExtensions.AddHealthChecks(IServiceCollection)"/>
    public static IHealthChecksBuilder AddWebJobsHealthChecks(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        // We cannot run any hosted services in Azure Functions, so the newly added one must be removed
        IHealthChecksBuilder builder = services.AddHealthChecks();
        ServiceDescriptor hostedService = services.Single(x => x.ServiceType.Name == "IHostedService" && x.ImplementationType?.Name == "HealthCheckPublisherHostedService");
        services.Remove(hostedService);

        return builder;
    }

    /// <summary>
    /// Configures the application insights telemetry for Azure Functions applications.
    /// </summary>
    /// <param name="services">The collection of services.</param>
    /// <param name="configure">A delegate for modifying the <see cref="TelemetryConfiguration"/> instance.</param>
    /// <returns>The given <paramref name="services"/> for additional methods to use.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection ConfigureWebJobsTelemetry(this IServiceCollection services, Action<IServiceProvider, TelemetryConfiguration> configure)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(configure, nameof(configure));

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ITelemetryModule, TelemetryConfigurationModule>(
                p => new TelemetryConfigurationModule(c => configure(p, c))));

        return services;
    }

    private sealed class TelemetryConfigurationModule : ITelemetryModule
    {
        private readonly Action<TelemetryConfiguration> _configure;

        public TelemetryConfigurationModule(Action<TelemetryConfiguration> configure)
            => _configure = EnsureArg.IsNotNull(configure, nameof(configure));

        public void Initialize(TelemetryConfiguration configuration)
            => _configure.Invoke(configuration);
    }
}
