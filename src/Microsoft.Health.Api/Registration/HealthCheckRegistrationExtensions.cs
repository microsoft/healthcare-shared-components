// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api;
using Microsoft.Health.Api.Features.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A collection of methods for configuring health checks.
/// </summary>
public static class HealthCheckRegistrationExtensions
{
    /// <summary>
    /// Configures the caching of health check results.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> containing the services.</param>
    /// <param name="configure">A delegate for configuring the cache.</param>
    /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection ConfigureHealthCheckCache(this IServiceCollection services, Action<HealthCheckCachingOptions> configure)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(configure, nameof(configure));

        services
            .AddOptions<HealthCheckCachingOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .Validate(x => x.Expiry >= x.RefreshOffset, Resources.InvalidHealthCheckCacheExpiry);

        return services;
    }

    /// <summary>
    /// Configures the global timeout for all health checks.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> containing the services.</param>
    /// <param name="timeout">The global health check timeout.</param>
    /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeout"/> is negative.
    /// </exception>
    public static IServiceCollection ConfigureHealthCheckTimeout(this IServiceCollection services, TimeSpan timeout)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsGte(timeout, TimeSpan.Zero, nameof(timeout));

        services.TryAdd(
            ServiceDescriptor.Transient<IPostConfigureOptions<HealthCheckServiceOptions>, HealthCheckTimeoutPostConfigure>(
                p => new HealthCheckTimeoutPostConfigure(timeout)));

        return services;
    }
}
