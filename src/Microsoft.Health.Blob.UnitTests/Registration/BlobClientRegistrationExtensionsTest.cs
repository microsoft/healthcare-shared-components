// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.IO;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Registration
{
    public class BlobClientRegistrationExtensionsTest
    {
        [Fact]
        public void GivenEmptyServiceCollection_WhenAddingBlobDataStore_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddBlobDataStore();

            Assert.True(services.ContainsSingleton<IHostedService, BlobHostedService>());
            Assert.True(services.ContainsSingleton<BlobServiceClient, BlobServiceClient>());
            Assert.True(services.ContainsSingleton<IBlobClientTestProvider, BlobClientReadWriteTestProvider>());
            Assert.True(services.ContainsSingleton<IBlobInitializer, BlobInitializer>());
            Assert.True(services.ContainsSingleton<RecyclableMemoryStreamManager>());

            // New
            Assert.True(services.ContainsSingleton<IPostConfigureOptions<BlobDataStoreConfiguration>>());
            Assert.True(services.ContainsTransient<IConfigureOptions<BlobDataStoreConfiguration>>());

            // Backward Compatability
            Assert.True(services.ContainsSingleton<BlobDataStoreConfiguration>());
        }

        [Fact]
        public void GivenEmptyServiceCollection_WhenAddingBlobServiceClient_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddBlobServiceClient();

            Assert.True(services.ContainsSingleton<IPostConfigureOptions<BlobDataStoreConfiguration>>());
            Assert.True(services.ContainsSingleton<BlobServiceClient>());
        }

        [Theory]
        [InlineData(null, BlobDataStoreAuthenticationType.ConnectionString, BlobLocalEmulator.ConnectionString)]
        [InlineData("foo", BlobDataStoreAuthenticationType.ConnectionString, "foo")]
        [InlineData(null, BlobDataStoreAuthenticationType.ManagedIdentity, null)]
        public void GivenConfigurationDelegate_WhenAddingBlobServiceClient_ThenUpdateConfigWithDefaults(
            string actualConnectionString,
            BlobDataStoreAuthenticationType authenticationType,
            string expectedConnectionString)
        {
            IConfiguration config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            services.AddSingleton(config);
            services.AddBlobServiceClient(
                c =>
                {
                    c.ConnectionString = actualConnectionString;
                    c.AuthenticationType = authenticationType;
                });

            BlobDataStoreConfiguration actual = services
                .BuildServiceProvider()
                .GetRequiredService<IOptions<BlobDataStoreConfiguration>>()
                .Value;

            Assert.Equal(expectedConnectionString, actual.ConnectionString);
            Assert.Equal(authenticationType, actual.AuthenticationType);
        }

        [Fact]
        public void GivenNoConnectionString_WhenAddingBlobDataStore_ThenUpdateConfig()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create("BlobStore:RequestOptions:ExponentialRetryMaxAttempts", "1"),
                    })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(config);
            services.AddBlobDataStore(c => c.RequestOptions.ServerTimeoutInMinutes = 100);

            BlobDataStoreConfiguration actual = services
                .BuildServiceProvider()
                .GetRequiredService<IOptions<BlobDataStoreConfiguration>>()
                .Value;

            Assert.Equal(BlobLocalEmulator.ConnectionString, actual.ConnectionString);
            Assert.Equal(BlobDataStoreAuthenticationType.ConnectionString, actual.AuthenticationType);
            Assert.Equal(1, actual.RequestOptions.ExponentialRetryMaxAttempts);
            Assert.Equal(100, actual.RequestOptions.ServerTimeoutInMinutes);
        }

        [Fact]
        public void GivenConnectionString_WhenAddingBlobDataStore_ThenUpdateConfig()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create("BlobStore:ConnectionString", "foo"),
                        KeyValuePair.Create("BlobStore:RequestOptions:DownloadMaximumConcurrency", "1"),
                    })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(config);
            services.AddBlobDataStore(c => c.RequestOptions.DownloadMaximumConcurrency = 2);

            BlobDataStoreConfiguration actual = services
                .BuildServiceProvider()
                .GetRequiredService<IOptions<BlobDataStoreConfiguration>>()
                .Value;

            Assert.Equal("foo", actual.ConnectionString);
            Assert.Equal(2, actual.RequestOptions.DownloadMaximumConcurrency);
        }
    }
}
