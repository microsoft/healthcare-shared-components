// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Blob.Configs;
using Microsoft.Health.Blob.Features.Storage;
using Microsoft.Health.Blob.Registration;
using Xunit;

namespace Microsoft.Health.Blob.UnitTests.Registration
{
    public class DefaultBlobDataStoreConfigurationTests
    {
        [Theory]
        [InlineData(null, BlobDataStoreAuthenticationType.ConnectionString, BlobLocalEmulator.ConnectionString)]
        [InlineData("foo", BlobDataStoreAuthenticationType.ConnectionString, "foo")]
        [InlineData(null, BlobDataStoreAuthenticationType.ManagedIdentity, null)]
        public void GivenBlobDataStoreConfig_WhenConfiguring_ThenEnsureProperDefaults(
            string actualConnectionString,
            BlobDataStoreAuthenticationType authenticationType,
            string expectedConnectionString)
        {
            var config = new BlobDataStoreConfiguration
            {
                AuthenticationType = authenticationType,
                ConnectionString = actualConnectionString,
            };

            DefaultBlobDataStoreConfiguration.Instance.Configure(config);

            Assert.Equal(expectedConnectionString, config.ConnectionString);
        }

        [Fact]
        public void GivenNoConfig_WhenConfiguringServices_ThenFetchDefaultValues()
        {
            var services = new ServiceCollection();

            services.AddOptions();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<BlobDataStoreConfiguration>>(new DefaultBlobDataStoreConfiguration()));

            BlobDataStoreConfiguration actual = services
                .BuildServiceProvider()
                .GetRequiredService<IOptions<BlobDataStoreConfiguration>>()
                .Value;

            Assert.Equal(BlobLocalEmulator.ConnectionString, actual.ConnectionString);
        }
    }
}
