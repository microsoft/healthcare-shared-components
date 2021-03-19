// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using EnsureThat;
using Microsoft.Health.Blob.Configs;
using Microsoft.IO;

namespace Microsoft.Health.Blob.Features.Storage
{
    public class BlobClientReadWriteTestProvider : IBlobClientTestProvider
    {
        private const string TestBlobName = "_testblob_";
        private const string TestBlobContent = "test-data";
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public BlobClientReadWriteTestProvider(RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));

            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        }

        public async Task PerformTestAsync(BlobServiceClient client, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(client, nameof(client));
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(blobContainerConfiguration, nameof(blobContainerConfiguration));

            BlobContainerClient blobContainer = client.GetBlobContainerClient(blobContainerConfiguration.ContainerName);
            BlockBlobClient blob = blobContainer.GetBlockBlobClient(TestBlobName);

            using var content = new MemoryStream(Encoding.UTF8.GetBytes(TestBlobContent));
            await blob.UploadAsync(
                content,
                new BlobHttpHeaders { ContentType = "text/plain" },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await DownloadBlobContentAsync(blob, cancellationToken).ConfigureAwait(false);
        }

        private async Task<byte[]> DownloadBlobContentAsync(BlockBlobClient blob, CancellationToken cancellationToken)
        {
            await using MemoryStream stream = _recyclableMemoryStreamManager.GetStream();

            await blob.DownloadToAsync(stream, cancellationToken).ConfigureAwait(false);
            return stream.ToArray();
        }
    }
}
