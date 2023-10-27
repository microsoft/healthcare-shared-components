// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Health;
using NSubstitute;
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

        _externalCredentialProvider.GetTokenCredential().Returns(new ManagedIdentityCredential());

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
}
