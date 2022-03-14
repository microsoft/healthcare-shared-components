// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.HealthChecks;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

public class HealthCheckCachingOptionsTests
{
    [Theory]
    [InlineData("-00:01:00")]
    [InlineData("3.00:00:00")]
    public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenValidateExpiry(string value)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create(nameof(HealthCheckCachingOptions.Expiry), value),
                })
            .Build();

        IServiceProvider provider = new ServiceCollection()
            .AddSingleton(config)
            .ConfigureHealthCheckCache(o => config.Bind(o))
            .BuildServiceProvider();

        IOptions<HealthCheckCachingOptions> options = provider.GetRequiredService<IOptions<HealthCheckCachingOptions>>();
        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    [Theory]
    [InlineData("-00:01:00")]
    [InlineData("3.00:00:00")]
    public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenValidateRefreshOffset(string value)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create(nameof(HealthCheckCachingOptions.RefreshOffset), value),
                })
            .Build();

        IServiceProvider provider = new ServiceCollection()
            .AddSingleton(config)
            .ConfigureHealthCheckCache(o => config.Bind(o))
            .BuildServiceProvider();

        IOptions<HealthCheckCachingOptions> options = provider.GetRequiredService<IOptions<HealthCheckCachingOptions>>();
        Assert.Throws<OptionsValidationException>(() => options.Value);
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

        IServiceProvider provider = new ServiceCollection()
            .AddSingleton(config)
            .ConfigureHealthCheckCache(o => config.Bind(o))
            .BuildServiceProvider();

        IOptions<HealthCheckCachingOptions> options = provider.GetRequiredService<IOptions<HealthCheckCachingOptions>>();
        Assert.Throws<OptionsValidationException>(() => options.Value);
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

        IServiceProvider provider = new ServiceCollection()
            .AddSingleton(config)
            .ConfigureHealthCheckCache(o => config.Bind(o))
            .BuildServiceProvider();

        IOptions<HealthCheckCachingOptions> options = provider.GetRequiredService<IOptions<HealthCheckCachingOptions>>();
        Assert.False(options.Value.CacheFailure);
        Assert.Equal(TimeSpan.FromSeconds(15), options.Value.Expiry);
        Assert.Equal(TimeSpan.FromSeconds(5), options.Value.RefreshOffset);
    }
}
