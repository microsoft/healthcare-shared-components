// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.HealthChecks;
using Microsoft.Health.Api.Modules;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck
{
    public class HealthCheckCachingOptionsTests
    {
        [Fact]
        public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenValidateExpiry()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.Expiry), "-00:01:00"),
                    })
                .Build();

            IOptions<HealthCheckCachingOptions> options = GetOptions(config);
            var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
            Assert.Single(exception.Failures);
        }

        [Fact]
        public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenValidateRefreshOffset()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.RefreshOffset), "-00:01:00"),
                    })
                .Build();

            IOptions<HealthCheckCachingOptions> options = GetOptions(config);
            var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
            Assert.Single(exception.Failures);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenValidateMaxRefreshThreads(string value)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.MaxRefreshThreads), value),
                    })
                .Build();

            IOptions<HealthCheckCachingOptions> options = GetOptions(config);
            var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
            Assert.Single(exception.Failures);
        }

        [Fact]
        public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenValidatePropertyCombination()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.Expiry), "00:00:10"),
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.RefreshOffset), "00:01:00"),
                    })
                .Build();

            IOptions<HealthCheckCachingOptions> options = GetOptions(config);
            var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
            Assert.Single(exception.Failures);
        }

        [Fact]
        public void GivenConfiguration_WhenCreatingHealthCheckOptionsWithMultipleProblems_ThenReturnAllFailures()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.Expiry), "-00:00:10"),
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.RefreshOffset), "-00:01:00"),
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.MaxRefreshThreads), "0"),
                    })
                .Build();

            IOptions<HealthCheckCachingOptions> options = GetOptions(config);
            var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
            Assert.Equal(3, exception.Failures.Count());
        }

        [Fact]
        public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenPopulateProperties()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.CacheFailure), "false"),
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.Expiry), "00:00:15"),
                        KeyValuePair.Create(nameof(HealthCheckCachingOptions.RefreshOffset), "00:00:05"),
                    })
                .Build();

            IOptions<HealthCheckCachingOptions> options = GetOptions(config);
            Assert.False(options.Value.CacheFailure);
            Assert.Equal(TimeSpan.FromSeconds(15), options.Value.Expiry);
            Assert.Equal(TimeSpan.FromSeconds(5), options.Value.RefreshOffset);
        }

        private static IOptions<HealthCheckCachingOptions> GetOptions(IConfiguration config)
        {
            IServiceCollection services = new ServiceCollection();

            new HealthCheckModule().Load(services);
            IServiceProvider provider = services
                .AddSingleton(config)
                .AddOptions<HealthCheckCachingOptions>()
                .Bind(config)
                .Services
                .BuildServiceProvider();

            return provider.GetRequiredService<IOptions<HealthCheckCachingOptions>>();
        }
    }
}
