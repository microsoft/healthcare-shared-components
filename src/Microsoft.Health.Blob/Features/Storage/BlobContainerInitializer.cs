// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Blob.Features.Storage
{
    public class BlobContainerInitializer : IBlobContainerInitializer
    {
        private readonly string _containerName;
        private readonly ILogger<BlobContainerInitializer> _logger;

        public BlobContainerInitializer(string containerName, ILogger<BlobContainerInitializer> logger)
        {
            EnsureArg.IsNotNullOrWhiteSpace(containerName, nameof(containerName));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _containerName = containerName;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<BlobContainerClient> InitializeContainerAsync(BlobServiceClient client)
        {
            EnsureArg.IsNotNull(client, nameof(client));

            BlobContainerClient container = client.GetBlobContainerClient(_containerName);

            _logger.LogDebug("Creating blob container if not exits: {containerName}", _containerName);
            await container.CreateIfNotExistsAsync();

            return container;
        }
    }
}
