// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    /// <summary>
    /// Provides methods for creating a BlobServiceClient instance and initializing containers.
    /// </summary>
    public interface IBlobClientInitializer
    {
        /// <summary>
        /// Creates an unopened <see cref="BlobServiceClient"/> based on the given <see cref="BlobDataStoreConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The connection string and requestion options.</param>
        /// <returns>A <see cref="BlobServiceClient"/> instance</returns>
        BlobServiceClient CreateBlobClient(BlobDataStoreConfiguration configuration);

        /// <summary>
        /// Open blobservice client
        /// </summary>
        /// <param name="client">The BlobService client</param>
        /// <param name="configuration">The data store config</param>
        /// <param name="blobContainerConfiguration">The container configuration to use for validating the blob client is open</param>
        Task OpenBlobClientAsync(BlobServiceClient client, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration);

        /// <summary>
        /// Initialize data store
        /// </summary>
        /// <param name="client">The <see cref="BlobServiceClient"/> instance to use for initialization.</param>
        /// <param name="configuration">The data store configuration.</param>
        /// <param name="containerInitializers">The blob container initializers.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task InitializeDataStoreAsync(BlobServiceClient client, BlobDataStoreConfiguration configuration, IEnumerable<IBlobContainerInitializer> containerInitializers);
    }
}
