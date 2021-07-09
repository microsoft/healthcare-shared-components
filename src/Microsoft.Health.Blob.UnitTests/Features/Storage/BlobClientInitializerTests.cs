// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Blob.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Features.Storage
{
    public class BlobClientInitializerTests
    {
        private const string TestContainerName1 = "testcontainer1";
        private const string TestContainerName2 = "testcontainer2";
        private readonly IBlobInitializer _blobInitializer;
        private readonly BlobServiceClient _blobClient;
        private readonly IBlobContainerInitializer _containerInitializer1;
        private readonly IBlobContainerInitializer _containerInitializer2;
        private readonly List<IBlobContainerInitializer> _collectionInitializers;
        private readonly BlobContainerClient _blobContainerClient1;
        private readonly BlobContainerClient _blobContainerClient2;

        public BlobClientInitializerTests()
        {
            _blobContainerClient1 = Substitute.For<BlobContainerClient>(new Uri("https://www.microsoft.com/"), new BlobClientOptions());
            _blobContainerClient2 = Substitute.For<BlobContainerClient>(new Uri("https://www.microsoft.com/"), new BlobClientOptions());

            IBlobClientTestProvider blobClientTestProvider = Substitute.For<IBlobClientTestProvider>();
            _blobClient = Substitute.For<BlobServiceClient>(new Uri("https://www.microsoft.com/"), null);
            _blobClient.GetBlobContainerClient(TestContainerName1).Returns(_blobContainerClient1);
            _blobClient.GetBlobContainerClient(TestContainerName2).Returns(_blobContainerClient2);

            _blobInitializer = new BlobInitializer(_blobClient, blobClientTestProvider, NullLogger<BlobInitializer>.Instance);
            _containerInitializer1 = Substitute.For<BlobContainerInitializer>(TestContainerName1, NullLogger<BlobContainerInitializer>.Instance);
            _containerInitializer2 = Substitute.For<BlobContainerInitializer>(TestContainerName2, NullLogger<BlobContainerInitializer>.Instance);
            _collectionInitializers = new List<IBlobContainerInitializer> { _containerInitializer1, _containerInitializer2 };
        }

        [Fact]
        public async void GivenMultipleCollections_WhenInitializing_ThenEachContainerInitializeMethodIsCalled()
        {
            await _blobInitializer.InitializeDataStoreAsync(_collectionInitializers);

            await _containerInitializer1.Received(1).InitializeContainerAsync(_blobClient);
            await _containerInitializer2.Received(1).InitializeContainerAsync(_blobClient);
        }

        [Fact]
        public async void GivenAConfiguration_WhenInitializing_ThenCreateContainerIfNotExistsIsCalled()
        {
            await _blobInitializer.InitializeDataStoreAsync(_collectionInitializers);

            await _blobContainerClient1.Received(1).CreateIfNotExistsAsync();
            await _blobContainerClient2.Received(1).CreateIfNotExistsAsync();
        }
    }
}
