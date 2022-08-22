// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.IO;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Registration;

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

    [Fact]
    public void GivenBlobDataStoreConfiguration_WhenAddingBlobDataStore_ThenMapInitializerSettings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        services.AddBlobDataStore(x =>
        {
            x.RequestOptions.InitialConnectMaxWaitInMinutes = 123;
            x.RequestOptions.InitialConnectWaitBeforeRetryInSeconds = 4567;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BlobInitializerOptions>>();
        Assert.Equal(TimeSpan.FromSeconds(4567), options.Value.RetryDelay);
        Assert.Equal(TimeSpan.FromMinutes(123), options.Value.Timeout);
    }

    [Fact]
    public void GivenNoConnectionString_WhenAddingBlobServiceClient_ThenUseDefault()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create("ConnectionString", (string)null),
                    KeyValuePair.Create("Operations:Download:MaximumConcurrency", "12"),
                })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(config);
        services.AddBlobServiceClient(config);

        BlobServiceClient actual = services
            .BuildServiceProvider()
            .GetRequiredService<BlobServiceClient>();

        Assert.Equal("devstoreaccount1", actual.AccountName);
    }

    [Fact]
    public async Task GivenServices_WhenConfiguringContainerInitialization_ThenRegisterAppropriateServices()
    {
        var services = new ServiceCollection();

        services
            .AddLogging()
            .AddBlobContainerInitialization(x =>
            {
                x.RetryDelay = TimeSpan.FromSeconds(5);
                x.Timeout = TimeSpan.FromMinutes(1);
            })
            .ConfigureContainer("foo", x => x.ContainerName = "FooContainer")
            .ConfigureContainer("bar", x => x.ContainerName = "BarContainer")
            .ConfigureContainer("baz", x => x.ContainerName = "BazContainer");

        ServiceProvider provider = services.BuildServiceProvider();

        IOptionsMonitor<BlobContainerConfiguration> optionsMonitor = provider.GetRequiredService<IOptionsMonitor<BlobContainerConfiguration>>();
        Assert.Equal("FooContainer", optionsMonitor.Get("foo").ContainerName);
        Assert.Equal("BarContainer", optionsMonitor.Get("bar").ContainerName);
        Assert.Equal("BazContainer", optionsMonitor.Get("baz").ContainerName);

        List<IBlobContainerInitializer> intializers = provider.GetRequiredService<IEnumerable<IBlobContainerInitializer>>().ToList();

        Assert.Equal(3, intializers.Count);
        await AssertBlobInitializationAsync(intializers[0], "FooContainer").ConfigureAwait(false);
        await AssertBlobInitializationAsync(intializers[1], "BarContainer").ConfigureAwait(false);
        await AssertBlobInitializationAsync(intializers[2], "BazContainer").ConfigureAwait(false);
    }

    [Fact]
    public void GivenServices_WhenConfiguringTransportOverride_ThenCreateNewTransport()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create("TransportOverride:ConnectTimeout", TimeSpan.FromSeconds(2).ToString()),
                })
            .Build();

        var services = new ServiceCollection();

        IServiceProvider provider = services
            .AddLogging()
            .AddBlobServiceClient(config)
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptionsMonitor<BlobClientOptions>>();
        Assert.NotSame(HttpClientTransport.Shared, options.CurrentValue.Transport);
    }

    private static async Task AssertBlobInitializationAsync(IBlobContainerInitializer initializer, string expectedContainer)
    {
        BlobServiceClient client = Substitute.For<BlobServiceClient>();
        BlobContainerClient containerClient = Substitute.For<BlobContainerClient>();
        client.GetBlobContainerClient(expectedContainer).Returns(containerClient);

        await initializer.InitializeContainerAsync(client).ConfigureAwait(false);

        client.Received(1).GetBlobContainerClient(expectedContainer);
        await containerClient.Received(1).CreateIfNotExistsAsync().ConfigureAwait(false);
    }
}
