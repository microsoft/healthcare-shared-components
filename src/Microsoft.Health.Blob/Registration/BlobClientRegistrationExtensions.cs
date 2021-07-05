// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Blob.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring a <see cref="BlobServiceClient"/> in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class BlobClientRegistrationExtensions
    {
        [Obsolete("Please use " + nameof(AddInitializedBlobServiceClient) + " instead.")]
        public static IServiceCollection AddBlobDataStore(this IServiceCollection services, Action<BlobDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            if (services.Any(x => x.ImplementationType == typeof(BlobClientProvider)))
            {
                return services;
            }

            services.AddSingleton(
                provider =>
                {
                    var config = new BlobDataStoreConfiguration();
                    provider
                        .GetService<IConfiguration>()
                        .GetSection(BlobDataStoreConfiguration.SectionName)
                        .Bind(config);

                    configureAction?.Invoke(config);
                    DefaultBlobDataStoreConfiguration.Instance.Configure(config);

                    return config;
                });

            return configureAction == null
                ? services.AddInitializedBlobServiceClient()
                : services.AddInitializedBlobServiceClient(configureAction);
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddBlobServiceClient(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.AddOptions();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<BlobDataStoreConfiguration>>(new DefaultBlobDataStoreConfiguration()));
            services.TryAddSingleton(p => BlobClientFactory.Create(p.GetRequiredService<IOptions<BlobDataStoreConfiguration>>().Value));

            return services;
        }

        /// <summary>
        ///  Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobDataStoreConfiguration"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddBlobServiceClient(this IServiceCollection services, Action<BlobDataStoreConfiguration> configure)
        {
            EnsureArg.IsNotNull(configure, nameof(configure));

            return services
                .AddBlobServiceClient()
                .Configure(configure);
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/> whose container
        /// is already initialized by the time service can be resolved.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
        public static IServiceCollection AddInitializedBlobServiceClient(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<BlobDataStoreConfiguration>>(new DefaultBlobDataStoreConfiguration()));

            // Register BlobClientProvider and re-use the same implementation instance across multiple services.
            // Note that declaring the factory as a variable will also ensure the implementation type in the
            // ServiceDescriptor is correctly BlobClientProvider instead of the service type
            Func<IServiceProvider, BlobClientProvider> factory = p => p.GetRequiredService<BlobClientProvider>();
            services.TryAddSingleton<BlobClientProvider>();
            services.TryAddSingleton<IRequireInitializationOnFirstRequest>(factory);
            services.AddHostedService(factory);

            services.TryAddSingleton(p => p.GetRequiredService<BlobClientProvider>().CreateBlobClient());
            services.TryAddSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>();
            services.TryAddSingleton<IBlobClientInitializer, BlobClientInitializer>();
            services.TryAddSingleton<RecyclableMemoryStreamManager>();

            return services;
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/> whose container
        /// is already initialized by the time service can be resolved.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobDataStoreConfiguration"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddInitializedBlobServiceClient(
            this IServiceCollection services,
            Action<BlobDataStoreConfiguration> configure)
        {
            EnsureArg.IsNotNull(configure, nameof(configure));

            return services
                .AddInitializedBlobServiceClient()
                .Configure(configure);
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/> whose container
        /// is already initialized by the time service can be resolved.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configureDataStore">A delegate for configuring the <see cref="BlobDataStoreConfiguration"/>.</param>
        /// <param name="configureContainer">A delegate for configuring the <see cref="BlobContainerConfiguration"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/>, <paramref name="configureDataStore"/>, or <paramref name="configureContainer"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddInitializedBlobServiceClient(
            this IServiceCollection services,
            Action<BlobDataStoreConfiguration> configureDataStore,
            Action<BlobContainerConfiguration> configureContainer)
        {
            EnsureArg.IsNotNull(configureContainer, nameof(configureContainer));

            return services
                .AddInitializedBlobServiceClient(configureDataStore)
                .Configure(configureContainer);
        }
    }
}
