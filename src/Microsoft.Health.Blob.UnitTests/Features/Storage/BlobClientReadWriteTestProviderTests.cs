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

            var blobContainerClient1 = Substitute.For<BlobContainerClient>(new Uri("https://www.microsoft.com/"), new BlobClientOptions());
            blobContainerClient1.GetBlockBlobClient(Arg.Any<string>()).Returns(blockBlobClient);

            _blobClient = Substitute.For<BlobServiceClient>(new Uri("https://www.microsoft.com/"), null);
            _blobClient.GetBlobContainerClient(Arg.Any<string>()).Returns(blobContainerClient1);
        }

        [Fact]
        public async void GivenCancellation_WhenPerformingTest_ThenExceptionIsHandled()
        {
            var testProvider = new BlobClientReadWriteTestProvider(new IO.RecyclableMemoryStreamManager(), _logger);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var exception = await Record.ExceptionAsync(() => testProvider.PerformTestAsync(_blobClient, new Configs.BlobContainerConfiguration(), cancellationTokenSource.Token));
            Assert.Null(exception);
        }
    }
}
