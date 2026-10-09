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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

[TestClass]
public class HealthCheckCachingOptionsTests
{
    [TestMethod]
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
        Assert.ContainsSingle(exception.Failures);
    }

    [TestMethod]
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
        Assert.ContainsSingle(exception.Failures);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-5")]
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
        Assert.ContainsSingle(exception.Failures);
    }

    [TestMethod]
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
        Assert.ContainsSingle(exception.Failures);
    }

    [TestMethod]
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
        Assert.AreEqual(3, exception.Failures.Count());
    }

    [TestMethod]
    public void GivenConfiguration_WhenCreatingHealthCheckOptions_ThenPopulateProperties()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new KeyValuePair<string, string>[]
                {
                    KeyValuePair.Create(nameof(HealthCheckCachingOptions.Expiry), "00:00:15"),
                    KeyValuePair.Create(nameof(HealthCheckCachingOptions.RefreshOffset), "00:00:05"),
                    KeyValuePair.Create(nameof(HealthCheckCachingOptions.MaxRefreshThreads), "4"),
                })
            .Build();

        HealthCheckCachingOptions options = GetOptions(config)?.Value;
        Assert.AreEqual(TimeSpan.FromSeconds(15), options.Expiry);
        Assert.AreEqual(TimeSpan.FromSeconds(5), options.RefreshOffset);
        Assert.AreEqual(4, options.MaxRefreshThreads);
    }

    [TestMethod]
    public void GivenNoConfiguration_WhenCreatingHealthCheckOptions_ThenPopulateDefault()
    {
        IServiceCollection services = new ServiceCollection();
        new HealthCheckModule().Load(services);
        IServiceProvider provider = services.BuildServiceProvider();

        HealthCheckCachingOptions options = provider.GetRequiredService<IOptions<HealthCheckCachingOptions>>()?.Value;
        Assert.AreEqual(TimeSpan.FromSeconds(1), options.Expiry);
        Assert.AreEqual(TimeSpan.Zero, options.RefreshOffset);
        Assert.AreEqual(2, options.MaxRefreshThreads);
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
