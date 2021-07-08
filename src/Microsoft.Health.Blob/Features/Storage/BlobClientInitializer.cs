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
            return BlobClientFactory.Create(configuration);
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
