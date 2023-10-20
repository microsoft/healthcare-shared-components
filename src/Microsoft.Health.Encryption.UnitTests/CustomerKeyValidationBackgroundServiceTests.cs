// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Health;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Encryption.UnitTests;

public class CustomerKeyValidationBackgroundServiceTests : IDisposable
{
    private readonly ICustomerKeyTestProvider _keyWrapUnwrapTestProvider = Substitute.For<ICustomerKeyTestProvider>();
    private readonly ICustomerKeyTestProvider _dataStoreStateTestProvider = Substitute.For<ICustomerKeyTestProvider>();

    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions = new CustomerManagedKeyOptions { KeyName = "test" };

    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache = new ValueCache<CustomerKeyHealth>();
    private readonly CustomerKeyValidationBackgroundService _validationService;
    private bool _disposedValue;

    public CustomerKeyValidationBackgroundServiceTests()
    {
        IOptions<CustomerManagedKeyOptions> cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(_customerManagedKeyOptions);

        _keyWrapUnwrapTestProvider.Priority.Returns(1);
        _keyWrapUnwrapTestProvider.FailureReason.Returns(HealthStatusReason.CustomerManagedKeyAccessLost);
        _keyWrapUnwrapTestProvider.AssertHealthAsync(Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        _dataStoreStateTestProvider.Priority.Returns(2);
        _dataStoreStateTestProvider.FailureReason.Returns(HealthStatusReason.DataStoreStateDegraded);
        _dataStoreStateTestProvider.AssertHealthAsync(Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        _validationService = new CustomerKeyValidationBackgroundService(
            new List<ICustomerKeyTestProvider>() { _keyWrapUnwrapTestProvider, _dataStoreStateTestProvider },
            _customerKeyHealthCache,
            cmkOptions,
            NullLogger<CustomerKeyValidationBackgroundService>.Instance);
    }

    [Fact]
    public async Task GivenKeyIsAccessible_WhenHealthIsChecked_ThenHealthyStateShouldBeSaved()
    {
        await _validationService.CheckHealth(CancellationToken.None);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetAsync();
        Assert.True(cmkHealth.IsHealthy);
        Assert.Null(cmkHealth.Exception);
        Assert.Equal(HealthStatusReason.None, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenKeyAccessFails_WhenHealthIsChecked_ThenNotHealthStateIsSaved()
    {
        var rfe = new RequestFailedException("key request failed");
        _keyWrapUnwrapTestProvider.AssertHealthAsync().Returns(new CustomerKeyHealth()
        {
            IsHealthy = false,
            Reason = HealthStatusReason.CustomerManagedKeyAccessLost,
            Exception = rfe,
        });

        await _validationService.CheckHealth(CancellationToken.None);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetAsync();

        Assert.False(cmkHealth.IsHealthy);
        Assert.Equal(rfe, cmkHealth.Exception);
        Assert.Equal(HealthStatusReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenDataStoreStateFails_WhenHealthIsChecked_ThenNotHealthStateIsSaved()
    {
        _dataStoreStateTestProvider.AssertHealthAsync().Returns(new CustomerKeyHealth()
        {
            IsHealthy = false,
            Reason = HealthStatusReason.DataStoreStateDegraded,
            Exception = new AggregateException(),
        });

        await _validationService.CheckHealth(CancellationToken.None);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetAsync();

        Assert.False(cmkHealth.IsHealthy);
        Assert.Equal(HealthStatusReason.DataStoreStateDegraded, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenAllTestProvidersFail_WhenHealthIsChecked_HighestPriorityHealthIsSaved()
    {
        _dataStoreStateTestProvider.AssertHealthAsync().Returns(new CustomerKeyHealth()
        {
            IsHealthy = false,
            Reason = HealthStatusReason.DataStoreStateDegraded,
            Exception = new AggregateException(),
        });

        var rfe = new RequestFailedException("key request failed");
        _keyWrapUnwrapTestProvider.AssertHealthAsync().Returns(new CustomerKeyHealth()
        {
            IsHealthy = false,
            Reason = HealthStatusReason.CustomerManagedKeyAccessLost,
            Exception = rfe,
        });

        await _validationService.CheckHealth(CancellationToken.None);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetAsync();

        Assert.False(cmkHealth.IsHealthy);
        Assert.Equal(rfe, cmkHealth.Exception);
        Assert.Equal(HealthStatusReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [Fact]
    public async Task GivenUninitializedHealthStatus_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        // health is not initialized
        Task<CustomerKeyHealth> cmkHealthTask = _customerKeyHealthCache.GetAsync();
        Assert.True(!cmkHealthTask.IsCompleted);

        // check health
        await _validationService.CheckHealth(CancellationToken.None);

        // health has been set, result is returned
        CustomerKeyHealth cmkHealth = await cmkHealthTask;
        Assert.True(cmkHealth.IsHealthy);
        Assert.Null(cmkHealth.Exception);
        Assert.Equal(HealthStatusReason.None, cmkHealth.Reason);
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
