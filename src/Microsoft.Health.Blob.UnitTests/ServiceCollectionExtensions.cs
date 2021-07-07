// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Health.Blob.UnitTests
{
    internal static class ServiceCollectionExtensions
    {
        public static bool ContainsTransient<TService>(this IServiceCollection services)
            => services.ContainsService<TService>(ServiceLifetime.Transient);

        public static bool ContainsTransient<TService, TImplementation>(this IServiceCollection services)
            => services.ContainsService<TService, TImplementation>(ServiceLifetime.Transient);

        public static bool ContainsSingleton<TService>(this IServiceCollection services)
            => services.ContainsService<TService>(ServiceLifetime.Singleton);

        public static bool ContainsSingleton<TService, TImplementation>(this IServiceCollection services)
            => services.ContainsService<TService, TImplementation>(ServiceLifetime.Singleton);

        public static bool ContainsService<TService>(this IServiceCollection services, ServiceLifetime lifetime)
        {
            EnsureArg.IsNotNull(services);

            return services.Any(x => x.Lifetime == lifetime && x.ServiceType == typeof(TService));
        }

        public static bool ContainsService<TService, TImplementation>(this IServiceCollection services, ServiceLifetime lifetime)
        {
            EnsureArg.IsNotNull(services);

            return services.Any(x =>
                x.Lifetime == lifetime &&
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
}
