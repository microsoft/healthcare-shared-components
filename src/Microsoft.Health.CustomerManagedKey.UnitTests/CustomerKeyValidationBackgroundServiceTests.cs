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

    private readonly ICustomerManagedKeyStatusCache _customerManagedKeyStatus = new CustomerManagedKeyStatusCache();
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
    public async Task GivenKeyIsAccessible_WhenHealthIsChecked_ThenHealthyStateShouldBeSaved()
    {
        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        IExternalResourceHealth cmkHealth = await _customerManagedKeyStatus.GetCachedData().ConfigureAwait(false);
        Assert.True(cmkHealth.IsHealthy);
        Assert.Null(cmkHealth.Description);
        Assert.Null(cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.None, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyAccessFails_WhenHealthIsChecked_ThenNotHealthStateIsSaved()
    {
        RequestFailedException requestFailedException = new RequestFailedException("Key is not accessible");
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(requestFailedException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        IExternalResourceHealth cmkHealth = await _customerManagedKeyStatus.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(requestFailedException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyOperationIsInvalid_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        InvalidOperationException invalidOperationException = new InvalidOperationException();
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(invalidOperationException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        IExternalResourceHealth cmkHealth = await _customerManagedKeyStatus.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(invalidOperationException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyOperationIsNotSupported_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        NotSupportedException notSupportedException = new NotSupportedException();
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(notSupportedException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        IExternalResourceHealth cmkHealth = await _customerManagedKeyStatus.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(notSupportedException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyWrapUnwrapFails_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        CryptographicException cryptoException = new CryptographicException();
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(cryptoException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        IExternalResourceHealth cmkHealth = await _customerManagedKeyStatus.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(cryptoException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenUninitializedHealthStatus_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        // health is not initialized
        Task<IExternalResourceHealth> cmkHealthTask = _customerManagedKeyStatus.GetCachedData();
        Assert.True(!cmkHealthTask.IsCompleted);

        // check health
        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        // health has been set, result is returned
        IExternalResourceHealth cmkHealth = await cmkHealthTask.ConfigureAwait(false);
        Assert.True(cmkHealth.IsHealthy);
        Assert.Null(cmkHealth.Description);
        Assert.Null(cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.None, cmkHealth.Reason);
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
