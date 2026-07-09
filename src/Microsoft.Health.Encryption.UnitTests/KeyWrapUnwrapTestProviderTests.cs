// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Encryption.UnitTests;

public class KeyWrapUnwrapTestProviderTests
{
    private readonly IExternalCredentialProvider _externalCredentialProvider = Substitute.For<IExternalCredentialProvider>();
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions = new CustomerManagedKeyOptions { KeyName = "", KeyVaultUri = null };

    private readonly KeyWrapUnwrapTestProvider _keyWrapUnwrapTestProvider;

    public KeyWrapUnwrapTestProviderTests()
    {
        IOptions<CustomerManagedKeyOptions> cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(_customerManagedKeyOptions);

        _externalCredentialProvider.GetTokenCredential().Returns(new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned));

        _keyWrapUnwrapTestProvider = new KeyWrapUnwrapTestProvider(_externalCredentialProvider, cmkOptions, NullLogger<KeyWrapUnwrapTestProvider>.Instance);
    }

    [Fact]
    public async Task CustomerKeyNotSet_AssertHealthAsync_HealthyReturned()
    {
        CustomerKeyHealth health = await _keyWrapUnwrapTestProvider.AssertHealthAsync();

        Assert.True(health.IsHealthy);
        Assert.Equal(HealthStatusReason.None, health.Reason);
        Assert.Null(health.Exception);
    }

    [Fact]
    public async Task GivenKeyVaultDnsFailure_WhenAssertHealthAsync_ThenUnhealthyReturned()
    {
        // Arrange
        const string aggregateMessage = "Retry failed after 4 tries. Retry settings can be adjusted in ClientOptions.Retry or by configuring a custom retry policy in ClientOptions.RetryPolicy. (Name or service not known (name-kv.vault.azure.net:443)) (Name or service not known (name-kv.vault.azure.net:443)) (Name or service not known (name-kv.vault.azure.net:443)) (Name or service not known (name-kv.vault.azure.net:443))";

        var innerException = new HttpRequestException("Name or service not known (name-kv.vault.azure.net:443)");
        var aggregateException = new AggregateException(aggregateMessage, innerException, innerException, innerException, innerException);

        KeyClient mockKeyClient = Substitute.For<KeyClient>();
        mockKeyClient.GetKeyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(aggregateException);

        var cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(new CustomerManagedKeyOptions { KeyName = "test-key", KeyVaultUri = new Uri("https://name-kv.vault.azure.net") });

        var provider = new KeyWrapUnwrapTestProvider(mockKeyClient, cmkOptions, NullLogger<KeyWrapUnwrapTestProvider>.Instance);

        // Act
        CustomerKeyHealth health = await provider.AssertHealthAsync();

        // Assert
        Assert.False(health.IsHealthy);
        Assert.Equal(HealthStatusReason.CustomerManagedKeyAccessLost, health.Reason);
        Assert.IsType<AggregateException>(health.Exception);
    }
}
