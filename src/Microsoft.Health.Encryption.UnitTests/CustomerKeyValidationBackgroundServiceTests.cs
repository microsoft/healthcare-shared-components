// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.Encryption.UnitTests;

public class CustomerKeyValidationBackgroundServiceTests : IDisposable
{
    private readonly IKeyTestProvider _keyTestProvider = Substitute.For<IKeyTestProvider>();
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions = new CustomerManagedKeyOptions { KeyName = "test" };

    private readonly AsyncData<CustomerKeyHealth> _customerKeyHealthCache = new AsyncData<CustomerKeyHealth>();
    private readonly CustomerKeyValidationBackgroundService _validationService;
    private bool _disposedValue;

    public CustomerKeyValidationBackgroundServiceTests()
    {
        IOptions<CustomerManagedKeyOptions> cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(_customerManagedKeyOptions);

        _keyTestProvider.PerformTestAsync(Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        _validationService = new CustomerKeyValidationBackgroundService(
            _keyTestProvider,
            _customerKeyHealthCache,
            NullLogger<CustomerKeyValidationBackgroundService>.Instance);
    }

    [Fact]
    public async Task GivenKeyIsAccessible_WhenHealthIsChecked_ThenHealthyStateShouldBeSaved()
    {
        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetCachedData().ConfigureAwait(false);
        Assert.True(cmkHealth.IsHealthy);
        Assert.Null(cmkHealth.Description);
        Assert.Null(cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.None, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyAccessFails_WhenHealthIsChecked_ThenNotHealthStateIsSaved()
    {
        RequestFailedException requestFailedException = new RequestFailedException("Key is not accessible");
        _keyTestProvider.PerformTestAsync().ThrowsForAnyArgs(requestFailedException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(requestFailedException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyOperationIsInvalid_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        InvalidOperationException invalidOperationException = new InvalidOperationException();
        _keyTestProvider.PerformTestAsync().ThrowsForAnyArgs(invalidOperationException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(invalidOperationException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyOperationIsNotSupported_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        NotSupportedException notSupportedException = new NotSupportedException();
        _keyTestProvider.PerformTestAsync().ThrowsForAnyArgs(notSupportedException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(notSupportedException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyWrapUnwrapFails_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        CryptographicException cryptoException = new CryptographicException();
        _keyTestProvider.PerformTestAsync().ThrowsForAnyArgs(cryptoException);

        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetCachedData().ConfigureAwait(false);
        Assert.False(cmkHealth.IsHealthy);
        Assert.NotNull(cmkHealth.Description);
        Assert.Equal(cryptoException, cmkHealth.Exception);
        Assert.Equal(ExternalHealthReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenUninitializedHealthStatus_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        // health is not initialized
        Task<CustomerKeyHealth> cmkHealthTask = _customerKeyHealthCache.GetCachedData();
        Assert.True(!cmkHealthTask.IsCompleted);

        // check health
        await _validationService.CheckHealth(CancellationToken.None).ConfigureAwait(false);

        // health has been set, result is returned
        CustomerKeyHealth cmkHealth = await cmkHealthTask.ConfigureAwait(false);
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
