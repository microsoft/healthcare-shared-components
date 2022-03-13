// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Health.SqlServer.UnitTests;

internal static class ServiceCollectionExtensions
{
    public static bool ContainsScoped<TService>(this IServiceCollection services)
        => services.ContainsService<TService>(ServiceLifetime.Scoped);

    public static bool ContainsScoped<TService, TImplementation>(this IServiceCollection services)
        => services.ContainsService<TService, TImplementation>(ServiceLifetime.Scoped);

    public static bool ContainsSingleton<TService>(this IServiceCollection services)
        => services.ContainsService<TService>(ServiceLifetime.Singleton);

    public static bool ContainsSingleton<TService, TImplementation>(this IServiceCollection services)
        => services.ContainsService<TService, TImplementation>(ServiceLifetime.Singleton);

    private static bool ContainsService<TService>(this IServiceCollection services, ServiceLifetime serviceLifetime)
        => services.ContainsService<TService, TService>(serviceLifetime);

    private static bool ContainsService<TService, TImplementation>(this IServiceCollection services, ServiceLifetime serviceLifetime)
    {
        EnsureArg.IsNotNull(services);

        return services.Any(x =>
            x.Lifetime == serviceLifetime &&
            x.ServiceType == typeof(TService) &&
            GetImplementationType(x) == typeof(TImplementation));
    }

    private static Type GetImplementationType(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType != null)
        {
            return descriptor.ImplementationType;
        }
        else if (descriptor.ImplementationInstance != null)
        {
            return descriptor.ImplementationInstance.GetType();
        }
        else
        {
            // ImplementationFactory is Func<IServiceProvider, object>, so we'll need to
            // inspect the type of the value at runtime to get the real type (instead of object)
            return descriptor.ImplementationFactory.GetType().GetGenericArguments()[1];
        }
    }
}
