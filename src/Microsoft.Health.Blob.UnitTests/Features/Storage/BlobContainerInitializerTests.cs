// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Blob.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Storage
{
    public class BlobContainerInitializerTests
    {
        private const string TestContainerName1 = "testcontainer1";
        private readonly NullLogger<BlobContainerInitializer> _logger;
        private readonly BlobServiceClient _blobClient;

        public BlobContainerInitializerTests()
        {
            _logger = new NullLogger<BlobContainerInitializer>();

            var blobContainerClient1 = Substitute.For<BlobContainerClient>(new Uri("https://www.microsoft.com/"), new BlobClientOptions());
            blobContainerClient1.CreateIfNotExistsAsync(cancellationToken: Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                    return Substitute.For<Task<Response<BlobContainerInfo>>>();
                });

            _blobClient = Substitute.For<BlobServiceClient>(new Uri("https://www.microsoft.com/"), null);
            _blobClient.GetBlobContainerClient(TestContainerName1).Returns(blobContainerClient1);
        }

        [Fact]
        public async void GivenCancellation_WhenInitializingContainer_ThenExceptionIsHandled()
        {
            var blobContainerInitializer = new BlobContainerInitializer(TestContainerName1, _logger);
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
            cancellationTokenSource.Cancel();

            var exception = await Record.ExceptionAsync(() => blobContainerInitializer.InitializeContainerAsync(_blobClient, cancellationTokenSource.Token));
            Assert.Null(exception);
        }
    }
}
