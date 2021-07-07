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
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring a <see cref="BlobServiceClient"/> in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class BlobClientRegistrationExtensions
    {
        [Obsolete("Please use " + nameof(AddBlobServiceClient) + " and " + nameof(AddBlobContainerInitialization) + " instead.")]
        public static IServiceCollection AddBlobDataStore(this IServiceCollection services, Action<BlobDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            if (services.Any(x => x.ImplementationType == typeof(BlobClientProvider)))
            {
                return services;
            }

            // Populate BlobDataStoreConfiguration from the configuration first, then the user delegate if provided
            OptionsBuilder<BlobDataStoreConfiguration> optionsBuilder = services
                .AddOptions<BlobDataStoreConfiguration>()
                .Configure<IConfiguration>((options, config) => config
                    .GetSection(BlobDataStoreConfiguration.SectionName)
                    .Bind(options));

            if (configureAction != null)
            {
                optionsBuilder.Configure(configureAction);
            }

            // Add the configuration directly for backwards compatibility along with other services
            services.TryAddSingleton(p => p.GetRequiredService<IOptions<BlobDataStoreConfiguration>>().Value);

            services
                .AddBlobServiceClient()
                .AddBlobContainerInitialization();

            return services;
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddBlobServiceClient(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services
                .AddOptions<BlobDataStoreConfiguration>()
                .PostConfigure(
                    options =>
                    {
                        if (string.IsNullOrEmpty(options.ConnectionString) && options.AuthenticationType == BlobDataStoreAuthenticationType.ConnectionString)
                        {
                            options.ConnectionString = BlobLocalEmulator.ConnectionString;
                        }
                    });

            services.TryAddSingleton(
                p =>
                {
                    BlobClientProvider blobClientProvider = p.GetService<BlobClientProvider>();
                    return blobClientProvider != null
                        ? blobClientProvider.CreateBlobClient()
                        : BlobClientFactory.Create(p.GetRequiredService<IOptions<BlobDataStoreConfiguration>>().Value);
                });

            return services;
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobDataStoreConfiguration"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddBlobServiceClient(this IServiceCollection services, Action<BlobDataStoreConfiguration> configure)
        {
            EnsureArg.IsNotNull(configure, nameof(configure));

            return services
                .AddBlobServiceClient()
                .Configure(configure);
        }

        /// <summary>
        /// Configures the singleton <see cref="BlobServiceClient"/> to only resolve once its container
        /// has been initialized in the background.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddBlobContainerInitialization(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            // Note that AddOptions is invoked in AddBlobServiceClient and is unnecessary here

            // Register BlobClientProvider and re-use the same implementation instance across multiple services.
            // Note that declaring the factory as a variable will also ensure the implementation type in the
            // ServiceDescriptor is correctly BlobClientProvider instead of the service type
            Func<IServiceProvider, BlobClientProvider> factory = p => p.GetRequiredService<BlobClientProvider>();
            services.TryAddSingleton<BlobClientProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRequireInitializationOnFirstRequest, BlobClientProvider>(factory));
            services.AddHostedService(factory);

            services.TryAddSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>();
            services.TryAddSingleton<IBlobClientInitializer, BlobClientInitializer>();
            services.TryAddSingleton<RecyclableMemoryStreamManager>();

            return services;
        }

        /// <summary>
        /// Configures the singleton <see cref="BlobServiceClient"/> to only resolve once its container
        /// has been initialized in the background.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobContainerConfiguration"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddBlobContainerInitialization(this IServiceCollection services, Action<BlobContainerConfiguration> configure)
        {
            EnsureArg.IsNotNull(configure, nameof(configure));

            return services
                .AddBlobContainerInitialization()
                .Configure(configure);
        }
    }
}
