// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Blob.Registration;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Registration
{
    public class BlobClientBuilderExtensionsTests
    {
        [Fact]
        public void GivenBuilder_WhenInitializingContainer_ThenAddNewServices()
        {
            var builder = new BlobClientBuilder(new ServiceCollection());
            builder.InitializeContainer();

            Assert.True(builder.Services.ContainsSingleton<BlobServiceClient>());
            Assert.True(builder.Services.ContainsSingleton<BlobClientProvider>());
            Assert.True(builder.Services.ContainsSingleton<IHostedService, BlobClientProvider>());
            Assert.True(builder.Services.ContainsSingleton<IRequireInitializationOnFirstRequest, BlobClientProvider>());
            Assert.True(builder.Services.ContainsSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>());
            Assert.True(builder.Services.ContainsSingleton<IBlobClientInitializer, BlobClientInitializer>());
            Assert.True(builder.Services.ContainsSingleton<RecyclableMemoryStreamManager>());
        }
    }
}
