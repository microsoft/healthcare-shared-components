﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.IO;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring a <see cref="BlobServiceClient"/> in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class BlobClientRegistrationExtensions
    {
        public static IServiceCollection AddBlobDataStore(this IServiceCollection services, Action<BlobDataStoreConfiguration> configureAction = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            if (services.Any(x => x.ImplementationType == typeof(BlobHostedService)))
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

            services
                .AddOptions<BlobInitializerOptions>()
                .Configure<IOptions<BlobDataStoreConfiguration>>((newConfig, oldConfig) =>
                {
                    BlobDataStoreRequestOptions requestOptions = oldConfig?.Value.RequestOptions;
                    if (requestOptions != null)
                    {
                        newConfig.RetryDelay = TimeSpan.FromSeconds(requestOptions.InitialConnectWaitBeforeRetryInSeconds);
                        newConfig.Timeout = TimeSpan.FromMinutes(requestOptions.InitialConnectMaxWaitInMinutes);
                    }
                });

            services.AddBlobServiceClient();

            // Add the configuration directly for backwards compatibility along with other services
            services.TryAddSingleton(p => p.GetRequiredService<IOptions<BlobDataStoreConfiguration>>().Value);

            services.AddBlobContainerInitialization();

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
        public static IServiceCollection AddBlobServiceClient(this IServiceCollection services, Action<BlobDataStoreConfiguration> configure = null)
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

            services.TryAddSingleton(p => BlobClientFactory.Create(p.GetRequiredService<IOptions<BlobDataStoreConfiguration>>().Value));
            return configure == null ? services : services.Configure(configure);
        }

        /// <summary>
        /// Adds a singleton <see cref="BlobServiceClient"/> to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configuration">A configuration section representing the <see cref="BlobServiceClientOptions"/>.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobClientOptions"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddBlobServiceClient(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<BlobClientOptions> configure = null)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            // TODO: The underlying AddBlobServiceClient method should allow for the ConnectionString and credentials
            // to be bound later using options rather than reading them directly from the configuration object.
            var options = new BlobServiceClientOptions();
            configuration.Bind(options);

            services.AddAzureClients(
                builder =>
                {
                    // The field "ConnectionString," "Credential," "ClientId," and the phrase "managedidentity"
                    // are all from the underlying library's source code. These fields and logic would be used
                    // if the configuration was passed directly to the AddBlobServiceClient call.
                    IAzureClientBuilder<BlobServiceClient, BlobClientOptions> clientBuilder = builder
                        .AddBlobServiceClient(options.ConnectionString)
                        .ConfigureOptions(x => configuration.Bind(x));

                    if (configure != null)
                    {
                        clientBuilder = clientBuilder.ConfigureOptions(configure);
                    }

                    if (string.Equals(options.Credential, "managedidentity", StringComparison.OrdinalIgnoreCase))
                    {
                        clientBuilder.WithCredential(new ManagedIdentityCredential(options.ClientId));
                    }
                });

            return services;
        }

        /// <summary>
        /// Configures the singleton <see cref="BlobServiceClient"/> to only resolve once its container
        /// has been initialized in the background.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configure">A delegate for configuring the <see cref="BlobInitializerOptions"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddBlobContainerInitialization(this IServiceCollection services, Action<BlobInitializerOptions> configure = null)
        {
            services.TryAddSingleton<IBlobInitializer, BlobInitializer>();
            services.AddHostedService<BlobHostedService>();
            services.TryAddSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>();
            services.TryAddSingleton<RecyclableMemoryStreamManager>();

            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }
    }
}
