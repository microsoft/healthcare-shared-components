// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    /// <summary>
    /// Provides methods for performing tests against blob storage.
    /// </summary>
    public interface IBlobClientTestProvider
    {
        /// <summary>
        /// Performs a test against blob storage.
        /// </summary>
        /// <param name="client">Client to connect to blob storage.</param>
        /// <param name="blobContainerConfiguration">Configuration specific to one container.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task PerformTestAsync(BlobServiceClient client, BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default);
    }
}
