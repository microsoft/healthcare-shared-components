// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Health;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Encryption.UnitTests;

[TestClass]
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

    [TestMethod]
    public async Task CustomerKeyNotSet_AssertHealthAsync_HealthyReturned()
    {
        CustomerKeyHealth health = await _keyWrapUnwrapTestProvider.AssertHealthAsync();

        Assert.IsTrue(health.IsHealthy);
        Assert.AreEqual(HealthStatusReason.None, health.Reason);
        Assert.IsNull(health.Exception);
    }

    [TestMethod]
    public async Task GivenKeyVaultDnsFailure_WhenAssertHealthAsync_ThenUnhealthyReturned()
    {
        // Arrange - use a non-existent Key Vault URI to trigger a real DNS failure
        var cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(new CustomerManagedKeyOptions
        {
            KeyName = "test-key",
            KeyVaultUri = new Uri("https://does-not-exist-kv.vault.azure.net"),
        });

        var provider = new KeyWrapUnwrapTestProvider(_externalCredentialProvider, cmkOptions, NullLogger<KeyWrapUnwrapTestProvider>.Instance);

        // Act
        CustomerKeyHealth health = await provider.AssertHealthAsync();

        // Assert
        Assert.IsFalse(health.IsHealthy);
        Assert.AreEqual(HealthStatusReason.CustomerManagedKeyAccessLost, health.Reason);
        Assert.IsNotNull(health.Exception);
    }
}
