// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.CustomerManagedKey.Configs;
using Microsoft.Health.CustomerManagedKey.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.CustomerManagedKey.UnitTests;

public class CustomerKeyValidationBackgroundServiceTests : IDisposable
{
    private readonly KeyClient _keyClient = Substitute.For<KeyClient>(new Uri("https://keyvault.com"), new DefaultAzureCredential());
    private readonly IKeyTestProvider _keyTestProvider = Substitute.For<IKeyTestProvider>();
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions = new CustomerManagedKeyOptions { KeyName = "test" };

    private readonly ICustomerManagedKeyStatus _customerManagedKeyStatus = new CustomerManagedKeyStatus();
    private readonly CustomerKeyValidationBackgroundService _validationService;
    private bool _disposedValue;

    public CustomerKeyValidationBackgroundServiceTests()
    {
        IOptions<CustomerManagedKeyOptions> cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(_customerManagedKeyOptions);

        _keyTestProvider.PerformTestAsync(Arg.Any<KeyClient>(), Arg.Any<CustomerManagedKeyOptions>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        _validationService = new CustomerKeyValidationBackgroundService(
            _keyClient,
            cmkOptions,
            _keyTestProvider,
            _customerManagedKeyStatus,
            NullLogger<CustomerKeyValidationBackgroundService>.Instance);
    }

    [Fact]
    public async Task GivenKeyIsAccessible_WhenHealthIsChecked_ThenHealthyStateShouldBeReturned()
    {
        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        Assert.True(_customerManagedKeyStatus.ExternalResourceHealth.IsHealthy);
        Assert.Null(_customerManagedKeyStatus.ExternalResourceHealth.Description);
        Assert.Null(_customerManagedKeyStatus.ExternalResourceHealth.Exception);
        Assert.Equal(ExternalHealthReason.None, _customerManagedKeyStatus.ExternalResourceHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyAccessFails_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        RequestFailedException requestFailedException = new RequestFailedException("Key is not accessible");
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(requestFailedException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        Assert.False(_customerManagedKeyStatus.ExternalResourceHealth.IsHealthy);
        Assert.NotNull(_customerManagedKeyStatus.ExternalResourceHealth.Description);
        Assert.Equal(requestFailedException, _customerManagedKeyStatus.ExternalResourceHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, _customerManagedKeyStatus.ExternalResourceHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyOperationIsInvalid_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        InvalidOperationException invalidOperationException = new InvalidOperationException();
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(invalidOperationException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        Assert.False(_customerManagedKeyStatus.ExternalResourceHealth.IsHealthy);
        Assert.NotNull(_customerManagedKeyStatus.ExternalResourceHealth.Description);
        Assert.Equal(invalidOperationException, _customerManagedKeyStatus.ExternalResourceHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, _customerManagedKeyStatus.ExternalResourceHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyOperationIsNotSupported_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        NotSupportedException notSupportedException = new NotSupportedException();
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(notSupportedException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        Assert.False(_customerManagedKeyStatus.ExternalResourceHealth.IsHealthy);
        Assert.NotNull(_customerManagedKeyStatus.ExternalResourceHealth.Description);
        Assert.Equal(notSupportedException, _customerManagedKeyStatus.ExternalResourceHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, _customerManagedKeyStatus.ExternalResourceHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyWrapUnwrapFails_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        CryptographicException cryptoException = new CryptographicException();
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(cryptoException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        Assert.False(_customerManagedKeyStatus.ExternalResourceHealth.IsHealthy);
        Assert.NotNull(_customerManagedKeyStatus.ExternalResourceHealth.Description);
        Assert.Equal(cryptoException, _customerManagedKeyStatus.ExternalResourceHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, _customerManagedKeyStatus.ExternalResourceHealth.Reason);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _validationService.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
