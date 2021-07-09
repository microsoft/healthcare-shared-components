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
    internal class BlobInitializer : IBlobInitializer
    {
        private readonly BlobServiceClient _client;
        private readonly IBlobClientTestProvider _testProvider;
        private readonly ILogger<BlobInitializer> _logger;

        public BlobInitializer(BlobServiceClient client, IBlobClientTestProvider testProvider, ILogger<BlobInitializer> logger)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(testProvider, nameof(testProvider));
            _client = client;
            _testProvider = testProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task InitializeDataStoreAsync(IEnumerable<IBlobContainerInitializer> containerInitializers)
        {
            EnsureArg.IsNotNull(containerInitializers, nameof(containerInitializers));

            try
            {
                _logger.LogInformation("Initializing Blob Storage and containers");

                foreach (IBlobContainerInitializer collectionInitializer in containerInitializers)
                {
                    await collectionInitializer.InitializeContainerAsync(_client);
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
        public async Task OpenBlobClientAsync(BlobContainerConfiguration blobContainerConfiguration)
        {
            EnsureArg.IsNotNull(blobContainerConfiguration, nameof(blobContainerConfiguration));

            _logger.LogInformation("Opening blob client connection to container {containerName}", blobContainerConfiguration.ContainerName);

            try
            {
                await _testProvider.PerformTestAsync(_client, blobContainerConfiguration);

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
