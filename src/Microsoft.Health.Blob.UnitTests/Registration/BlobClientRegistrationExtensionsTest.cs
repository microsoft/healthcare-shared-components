// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Blob.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Registration
{
    public class BlobClientRegistrationExtensionsTest
    {
        [Fact]
        [Obsolete]
        public void GivenEmptyServiceCollection_WhenAddingBlobDataStore_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddBlobDataStore();

            Assert.True(services.ContainsSingleton<BlobClientProvider>());
            Assert.True(services.ContainsSingleton<IHostedService, BlobClientProvider>());
            Assert.True(services.ContainsSingleton<IRequireInitializationOnFirstRequest, BlobClientProvider>());
            Assert.True(services.ContainsSingleton<BlobServiceClient, BlobServiceClient>());
            Assert.True(services.ContainsSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>());
            Assert.True(services.ContainsSingleton<IBlobClientInitializer, BlobClientInitializer>());
            Assert.True(services.ContainsSingleton<RecyclableMemoryStreamManager>());

            // New
            Assert.True(services.ContainsSingleton<IConfigureOptions<BlobDataStoreConfiguration>, DefaultBlobDataStoreConfiguration>());

            // Backward Compatability
            Assert.True(services.ContainsSingleton<BlobDataStoreConfiguration>());
        }

        [Fact]
        public void GivenEmptyServiceCollection_WhenAddingBlobServiceClient_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddBlobServiceClient();

            Assert.True(services.ContainsSingleton<IConfigureOptions<BlobDataStoreConfiguration>, DefaultBlobDataStoreConfiguration>());
            Assert.True(services.ContainsSingleton<BlobServiceClient>());
        }

        [Fact]
        public void GivenEmptyServiceCollection_WhenAddingInitializedBlobServiceClient_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddInitializedBlobServiceClient();

            Assert.True(services.ContainsSingleton<IConfigureOptions<BlobDataStoreConfiguration>, DefaultBlobDataStoreConfiguration>());
            Assert.True(services.ContainsSingleton<BlobClientProvider>());
            Assert.True(services.ContainsSingleton<IHostedService, BlobClientProvider>());
            Assert.True(services.ContainsSingleton<IRequireInitializationOnFirstRequest, BlobClientProvider>());
            Assert.True(services.ContainsSingleton<BlobServiceClient, BlobServiceClient>());
            Assert.True(services.ContainsSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>());
            Assert.True(services.ContainsSingleton<IBlobClientInitializer, BlobClientInitializer>());
            Assert.True(services.ContainsSingleton<RecyclableMemoryStreamManager>());
        }
    }
}
