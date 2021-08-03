// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Storage
{
    public class BlobClientReadWriteTestProviderTests
    {
        private readonly NullLogger<BlobClientReadWriteTestProvider> _logger;
        private readonly BlobServiceClient _blobClient;

        public BlobClientReadWriteTestProviderTests()
        {
            _logger = new NullLogger<BlobClientReadWriteTestProvider>();

            var blockBlobClient = Substitute.For<BlockBlobClient>();
            blockBlobClient.UploadAsync(Arg.Any<MemoryStream>(), Arg.Any<BlobHttpHeaders>(), null, null, null, null, Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                    return Substitute.For<Response<BlobContentInfo>>();
                });

            var blobContainerClient = Substitute.For<BlobContainerClient>(new Uri("https://www.microsoft.com/"), new BlobClientOptions());
            blobContainerClient.GetBlockBlobClient(Arg.Any<string>()).Returns(blockBlobClient);

            _blobClient = Substitute.For<BlobServiceClient>(new Uri("https://www.microsoft.com/"), null);
            _blobClient.GetBlobContainerClient(Arg.Any<string>()).Returns(blobContainerClient);
        }

        [Fact]
        public async void GivenCancelation_WhenPerformingTest_ThenOperationCanceledExceptionIsThrown()
        {
            var testProvider = new BlobClientReadWriteTestProvider(new RecyclableMemoryStreamManager(), _logger);
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => testProvider.PerformTestAsync(_blobClient, new Configs.BlobContainerConfiguration(), cancellationTokenSource.Token));
        }
    }
}
