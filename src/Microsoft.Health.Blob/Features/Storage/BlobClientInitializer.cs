// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    internal class BlobClientInitializer : IBlobClientInitializer
    {
        private readonly IBlobClientTestProvider _testProvider;
        private readonly ILogger<BlobClientInitializer> _logger;

        public BlobClientInitializer(IBlobClientTestProvider testProvider, ILogger<BlobClientInitializer> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(testProvider, nameof(testProvider));

            _testProvider = testProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public BlobServiceClient CreateBlobClient(BlobDataStoreConfiguration configuration)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));

            _logger.LogInformation("Creating BlobClient instance");

            // Configure the blob client default request options and retry logic
            var blobClientOptions = new BlobClientOptions();
            blobClientOptions.Retry.MaxRetries = configuration.RequestOptions.ExponentialRetryMaxAttempts;
            blobClientOptions.Retry.Mode = Azure.Core.RetryMode.Exponential;
            blobClientOptions.Retry.Delay = TimeSpan.FromSeconds(configuration.RequestOptions.ExponentialRetryBackoffDeltaInSeconds);
            blobClientOptions.Retry.NetworkTimeout = TimeSpan.FromMinutes(configuration.RequestOptions.ServerTimeoutInMinutes);

            return new BlobServiceClient(configuration.ConnectionString, blobClientOptions);
        }

        /// <inheritdoc />
        public async Task InitializeDataStoreAsync(BlobServiceClient client, BlobDataStoreConfiguration configuration, IEnumerable<IBlobContainerInitializer> containerInitializers)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(containerInitializers, nameof(containerInitializers));

            try
            {
                _logger.LogInformation("Initializing Blob Storage and containers");

                foreach (IBlobContainerInitializer collectionInitializer in containerInitializers)
                {
                    await collectionInitializer.InitializeContainerAsync(client);
                }

                _logger.LogInformation("Blob Storage and containers successfully initialized");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Blob Storage and containers initialization failed");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task OpenBlobClientAsync(BlobServiceClient client, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(blobContainerConfiguration, nameof(blobContainerConfiguration));

            _logger.LogInformation("Opening blob client connection to container {containerName}", blobContainerConfiguration.ContainerName);

            try
            {
                await _testProvider.PerformTestAsync(client, configuration, blobContainerConfiguration);

                _logger.LogInformation("Established blob client connection to container {containerName}", blobContainerConfiguration.ContainerName);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Failed to connect to blob client container {containerName}", blobContainerConfiguration.ContainerName);
                throw;
            }
        }
    }
}
