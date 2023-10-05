// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Core.Extensions;
using Xunit;

namespace Microsoft.Health.Core.UnitTests.Extensions;

public class IAzureClientBuilderExtensionsTests
{
    private static readonly Assembly AzureCoreAssembly = typeof(TokenCredential).Assembly;
    private static readonly Assembly AzureIdentityAssembly = typeof(DefaultAzureCredential).Assembly;

    [Fact]
    public void GivenNoConfiguration_WhenConfiguringManagedIdentity_ThenUseDefaults()
    {
        RetryOptions expectedOptions = CreateRetryOptions();

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("serviceUri", "https://127.0.0.1:10000/devstoreaccount1"),
                new KeyValuePair<string, string>("credential", "managedidentity"),
            })
            .Build();

        ManagedIdentityCredential actualCredential = AssertTokenCredential<ManagedIdentityCredential>(config);
        AssertRetryOptions(expectedOptions, actualCredential);
    }

    [Fact]
    public void GivenConfiguration_WhenConfiguringManagedIdentity_ThenUseRetrySettings()
    {
        RetryOptions expectedOptions = CreateRetryOptions(
            delay: TimeSpan.FromSeconds(2),
            maxDelay: TimeSpan.FromMinutes(1),
            maxRetries: 12,
            networkTimeout: TimeSpan.FromMinutes(5));

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("serviceUri", "https://127.0.0.1:10000/devstoreaccount1"),
                new KeyValuePair<string, string>("credential", "managedidentity"),
                new KeyValuePair<string, string>($"credentialRetry:{nameof(RetryOptions.Delay)}", expectedOptions.Delay.ToString()),
                new KeyValuePair<string, string>($"credentialRetry:{nameof(RetryOptions.MaxDelay)}", expectedOptions.MaxDelay.ToString()),
                new KeyValuePair<string, string>($"credentialRetry:{nameof(RetryOptions.MaxRetries)}", expectedOptions.MaxRetries.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>($"credentialRetry:{nameof(RetryOptions.Mode)}", expectedOptions.Mode.ToString()),
                new KeyValuePair<string, string>($"credentialRetry:{nameof(RetryOptions.NetworkTimeout)}", expectedOptions.NetworkTimeout.ToString()),
            })
            .Build();

        ManagedIdentityCredential actualCredential = AssertTokenCredential<ManagedIdentityCredential>(config);
        AssertRetryOptions(expectedOptions, actualCredential);
    }

    [Fact]
    public void GivenOtherCredentialType_WhenConfiguringCredential_ThenSkipAndUseDefaults()
    {
        RetryOptions expectedOptions = CreateRetryOptions();

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("serviceUri", "https://127.0.0.1:10000/devstoreaccount1"),
                new KeyValuePair<string, string>("clientId", Guid.NewGuid().ToString()),
                new KeyValuePair<string, string>($"credentialRetry:{nameof(RetryOptions.Delay)}", "00:52:00"),
                new KeyValuePair<string, string>($"credentialRetry:{nameof(RetryOptions.MaxRetries)}", "17"),
            })
            .Build();

        DefaultAzureCredential actualCredential = AssertTokenCredential<DefaultAzureCredential>(config);
        AssertRetryOptions(expectedOptions, actualCredential);
    }

    private static RetryOptions CreateRetryOptions(
            TimeSpan? delay = null,
            TimeSpan? maxDelay = null,
            int maxRetries = 3,
            RetryMode mode = RetryMode.Exponential,
            TimeSpan? networkTimeout = null)
    {
        var options = Activator.CreateInstance(typeof(RetryOptions), nonPublic: true) as RetryOptions;
        options.Delay = delay ?? TimeSpan.FromSeconds(0.8);
        options.MaxDelay = maxDelay ?? TimeSpan.FromMinutes(1);
        options.MaxRetries = maxRetries;
        options.Mode = mode;
        options.NetworkTimeout = networkTimeout ?? TimeSpan.FromSeconds(100);

        return options;
    }

    private static T AssertTokenCredential<T>(IConfiguration config) where T : TokenCredential
    {
        IServiceCollection services = new ServiceCollection();
        services.AddAzureClients(builder => builder
            .AddBlobServiceClient(config)
            .WithRetryableCredential(config));

        IServiceProvider serviceProvider = services.BuildServiceProvider();

        BlobServiceClient client = serviceProvider.GetRequiredService<BlobServiceClient>();
        var authenticationPolicy = typeof(BlobServiceClient)
            .GetProperty("AuthenticationPolicy", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(client) as BearerTokenAuthenticationPolicy;

        object accessTokenCache = typeof(BearerTokenAuthenticationPolicy)
            .GetField("_accessTokenCache", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(authenticationPolicy);

        Type accessTokenCacheType = typeof(BearerTokenAuthenticationPolicy).GetNestedType("AccessTokenCache", BindingFlags.NonPublic);
        var tokenCredential = accessTokenCacheType
            .GetField("_credential", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(accessTokenCache) as TokenCredential;

        return Assert.IsType<T>(tokenCredential);
    }

    private static void AssertRetryOptions<T>(RetryOptions expected, T actualCredential)
        where T : TokenCredential
    {
        // Unfortunately, much of this information is embedded in the internal HTTP pipeline,
        // so it takes a bit of effort to figure out whether classes were configured in the expectedOptions way.
        Type credentialPipelineType = AzureIdentityAssembly.GetType("Azure.Identity.CredentialPipeline", throwOnError: true);
        object credentialPipeline = typeof(T)
            .GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(actualCredential);

        var httpPipeline = credentialPipelineType
            .GetProperty("HttpPipeline", BindingFlags.Public | BindingFlags.Instance)
            .GetValue(credentialPipeline) as HttpPipeline;

        var policies = (ReadOnlyMemory<HttpPipelinePolicy>)typeof(HttpPipeline)
            .GetField("_pipeline", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(httpPipeline);

        // Validate RetryPolicy
        var actualRetryPolicy = policies.ToArray().Single(x => x.GetType() == typeof(RetryPolicy)) as RetryPolicy;
        var delayStrategy = typeof(RetryPolicy)
            .GetField("_delayStrategy", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(actualRetryPolicy) as DelayStrategy;

        int maxRetries = (int)typeof(RetryPolicy)
            .GetField("_maxRetries", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(actualRetryPolicy);

        Assert.Equal(expected.MaxRetries, maxRetries);

        Type expectedStrategyType = expected.Mode switch
        {
            RetryMode.Exponential => AzureCoreAssembly.GetType("Azure.Core.ExponentialDelayStrategy", throwOnError: true),
            RetryMode.Fixed => AzureCoreAssembly.GetType("Azure.Core.FixedDelayStrategy", throwOnError: true),
            _ => null,
        };

        if (expectedStrategyType is null)
            Assert.Fail($"Unexpected retry mode: {expected.Mode}");

        Assert.IsType(expectedStrategyType, delayStrategy);
        var delay = (TimeSpan)expectedStrategyType
            .GetField("_delay", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(delayStrategy);

        Assert.Equal(expected.Delay, delay);

        if (expected.Mode == RetryMode.Exponential)
        {
            var maxDelay = (TimeSpan)typeof(DelayStrategy)
                .GetField("_maxDelay", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(delayStrategy);

            Assert.Equal(expected.MaxDelay, maxDelay);
        }

        // Validate ResponseBodyPolicy
        Type responseBodyPolicyType = AzureCoreAssembly.GetType("Azure.Core.Pipeline.ResponseBodyPolicy", throwOnError: true);
        object actualResponseBodyPolicy = policies.ToArray().Single(x => x.GetType() == responseBodyPolicyType);
        var networkTimeout = (TimeSpan)responseBodyPolicyType
            .GetField("_networkTimeout", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(actualResponseBodyPolicy);

        Assert.Equal(expected.NetworkTimeout, networkTimeout);
    }
}
