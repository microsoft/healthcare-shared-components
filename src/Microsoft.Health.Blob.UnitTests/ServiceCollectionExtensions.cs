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
        public static bool ContainsSingleton<TService>(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services);

            return services.Any(x =>
                x.Lifetime == ServiceLifetime.Singleton &&
                x.ServiceType == typeof(TService));
        }

        public static bool ContainsSingleton<TService, TImplementation>(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services);

            return services.Any(x =>
                x.Lifetime == ServiceLifetime.Singleton &&
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
