// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Blob.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an existing <see cref="BlobServiceClient"/>.
    /// </summary>
    public static class BlobClientBuilderExtensions
    {
        /// <summary>
        /// Configures the singleton <see cref="BlobServiceClient"/> to only resolve once its container
        /// has been initialized in the background.
        /// </summary>
        /// <param name="builder">The <see cref="BlobClientBuilder"/> to be configured.</param>
        /// <returns>The <paramref name="builder"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static BlobClientBuilder InitializeContainer(this BlobClientBuilder builder)
        {
            IServiceCollection services = EnsureArg.IsNotNull(builder, nameof(builder)).Services;

            // Register BlobClientProvider and re-use the same implementation instance across multiple services.
            // Note that declaring the factory as a variable will also ensure the implementation type in the
            // ServiceDescriptor is correctly BlobClientProvider instead of the service type
            Func<IServiceProvider, BlobClientProvider> factory = p => p.GetRequiredService<BlobClientProvider>();
            services.TryAddSingleton<BlobClientProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRequireInitializationOnFirstRequest, BlobClientProvider>(factory));
            services.AddHostedService(factory);

            services.Replace(ServiceDescriptor.Singleton(p => p.GetRequiredService<BlobClientProvider>().CreateBlobClient()));
            services.TryAddSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>();
            services.TryAddSingleton<IBlobClientInitializer, BlobClientInitializer>();
            services.TryAddSingleton<RecyclableMemoryStreamManager>();

            return builder;
        }

        /// <summary>
        /// Configures the singleton <see cref="BlobServiceClient"/> to only resolve once its container
        /// has been initialized in the background.
        /// </summary>
        /// <param name="builder">The <see cref="BlobClientBuilder"/> to be configured.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobContainerConfiguration"/>.</param>
        /// <returns>The <paramref name="builder"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static BlobClientBuilder InitializeContainer(this BlobClientBuilder builder, Action<BlobContainerConfiguration> configure)
        {
            EnsureArg.IsNotNull(configure, nameof(configure));

            builder.InitializeContainer();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
