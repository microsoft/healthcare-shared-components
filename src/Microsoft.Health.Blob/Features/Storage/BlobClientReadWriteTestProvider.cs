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
using Microsoft.Extensions.Logging;
using Microsoft.Health.Blob.Configs;
using Microsoft.IO;

namespace Microsoft.Health.Blob.Features.Storage;

/// <summary>
/// Verifies read and write operations on a blob storage container.
/// </summary>
public class BlobClientReadWriteTestProvider : IBlobClientTestProvider
{
    private const string TestBlobName = "_testblob_";
    private const string TestBlobContent = "test-data";
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
    private readonly ILogger<BlobClientReadWriteTestProvider> _logger;

    public BlobClientReadWriteTestProvider(RecyclableMemoryStreamManager recyclableMemoryStreamManager, ILogger<BlobClientReadWriteTestProvider> logger)
    {
        EnsureArg.IsNotNull(recyclableMemoryStreamManager, nameof(recyclableMemoryStreamManager));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PerformTestAsync(BlobServiceClient client, BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(blobContainerConfiguration, nameof(blobContainerConfiguration));

        BlobContainerClient blobContainer = client.GetBlobContainerClient(blobContainerConfiguration.ContainerName);
        BlockBlobClient blob = blobContainer.GetBlockBlobClient(TestBlobName);

        _logger.LogInformation("Reading and writing blob: {Container}/{Blob}", blobContainerConfiguration.ContainerName, TestBlobName);
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(TestBlobContent));
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = "text/plain" },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        await DownloadBlobContentAsync(blob, cancellationToken).ConfigureAwait(false);
    }

    private async Task<byte[]> DownloadBlobContentAsync(BlockBlobClient blob, CancellationToken cancellationToken)
    {
        MemoryStream stream = _recyclableMemoryStreamManager.GetStream();
        await using (stream.ConfigureAwait(false))
        {
            await blob.DownloadToAsync(stream, cancellationToken).ConfigureAwait(false);
            return stream.ToArray();
        }
    }
}
