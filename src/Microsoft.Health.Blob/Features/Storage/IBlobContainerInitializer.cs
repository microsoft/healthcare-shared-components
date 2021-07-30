// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Microsoft.Health.Blob.Features.Storage
{
    /// <summary>
    /// Provides methods for initializing blob storage containers.
    /// </summary>
    public interface IBlobContainerInitializer
    {
        /// <summary>
        /// Initializes a blob storage container.
        /// </summary>
        /// <param name="client">The blob storage client.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        Task<BlobContainerClient> InitializeContainerAsync(BlobServiceClient client, CancellationToken cancellationToken = default);
    }
}
