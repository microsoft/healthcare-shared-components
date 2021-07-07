// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Blob.Registration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring a <see cref="BlobServiceClient"/> in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class BlobClientRegistrationExtensions
    {
        [Obsolete("Please use " + nameof(AddBlobServiceClient) + " with " + nameof(BlobClientBuilderExtensions.InitializeContainer) + " instead.")]
        public static IServiceCollection AddBlobDataStore(this IServiceCollection services, Action<BlobDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            if (services.Any(x => x.ImplementationType == typeof(BlobClientProvider)))
            {
                return services;
            }

            // Populate BlobDataStoreConfiguration from the configuration first, then the user-provided delegate
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<BlobDataStoreConfiguration>, ConfigureBlobClientFromConfigurationOptions>());

            BlobClientBuilder builder = configureAction == null
                ? services.AddBlobServiceClient()
                : services.AddBlobServiceClient(configureAction);

            builder.InitializeContainer();

            // Add the configuration directly for backwards compatibility along with other services
            services.TryAddSingleton(p => p.GetRequiredService<IOptions<BlobDataStoreConfiguration>>().Value);

            return services;
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <returns>A <see cref="BlobClientBuilder"/> for configuring the <see cref="BlobServiceClient"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
        public static BlobClientBuilder AddBlobServiceClient(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.AddOptions();
            services.PostConfigure<BlobDataStoreConfiguration>(
                options =>
                {
                    if (string.IsNullOrEmpty(options.ConnectionString) && options.AuthenticationType == BlobDataStoreAuthenticationType.ConnectionString)
                    {
                        options.ConnectionString = BlobLocalEmulator.ConnectionString;
                    }
                });
            services.TryAddSingleton(p => BlobClientFactory.Create(p.GetRequiredService<IOptions<BlobDataStoreConfiguration>>().Value));

            return new BlobClientBuilder(services);
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobDataStoreConfiguration"/>.</param>
        /// <returns>A <see cref="BlobClientBuilder"/> for configuring the <see cref="BlobServiceClient"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static BlobClientBuilder AddBlobServiceClient(this IServiceCollection services, Action<BlobDataStoreConfiguration> configure)
        {
            EnsureArg.IsNotNull(configure, nameof(configure));

            BlobClientBuilder builder = services.AddBlobServiceClient();
            services.Configure(configure);

            return builder;
        }
    }
}
