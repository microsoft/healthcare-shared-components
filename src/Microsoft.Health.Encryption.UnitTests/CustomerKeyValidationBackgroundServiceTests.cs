// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Configs;
using Microsoft.Health.Encryption.Customer.Health;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Encryption.UnitTests;

[TestClass]
public class CustomerKeyValidationBackgroundServiceTests : IDisposable
{
    private readonly IKeyTestProvider _keyWrapUnwrapTestProvider = Substitute.For<IKeyTestProvider>();

    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions = new CustomerManagedKeyOptions { KeyName = "test" };

    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache = new ValueCache<CustomerKeyHealth>();
    private readonly CustomerKeyValidationBackgroundService _validationService;
    private bool _disposedValue;

    public CustomerKeyValidationBackgroundServiceTests()
    {
        IOptions<CustomerManagedKeyOptions> cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(_customerManagedKeyOptions);

        _keyWrapUnwrapTestProvider.AssertHealthAsync(Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.FromResult(new CustomerKeyHealth());
            });

        _validationService = new CustomerKeyValidationBackgroundService(
            _keyWrapUnwrapTestProvider,
            _customerKeyHealthCache,
            cmkOptions,
            NullLogger<CustomerKeyValidationBackgroundService>.Instance);
    }

    [TestMethod]
    public async Task GivenKeyIsAccessible_WhenHealthIsChecked_ThenHealthyStateShouldBeSaved()
    {
        await _validationService.CheckHealth(CancellationToken.None);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetAsync();
        Assert.IsTrue(cmkHealth.IsHealthy);
        Assert.IsNull(cmkHealth.Exception);
        Assert.AreEqual(HealthStatusReason.None, cmkHealth.Reason);
    }

    [TestMethod]
    public async Task GivenKeyAccessFails_WhenHealthIsChecked_ThenNotHealthStateIsSaved()
    {
        var rfe = new RequestFailedException("key request failed");
        _keyWrapUnwrapTestProvider.AssertHealthAsync(Arg.Any<CancellationToken>()).Returns(new CustomerKeyHealth()
        {
            IsHealthy = false,
            Reason = HealthStatusReason.CustomerManagedKeyAccessLost,
            Exception = rfe,
        });

        await _validationService.CheckHealth(CancellationToken.None);

        CustomerKeyHealth cmkHealth = await _customerKeyHealthCache.GetAsync();

        Assert.IsFalse(cmkHealth.IsHealthy);
        Assert.AreEqual(rfe, cmkHealth.Exception);
        Assert.AreEqual(HealthStatusReason.CustomerManagedKeyAccessLost, cmkHealth.Reason);
    }

    [TestMethod]
    public async Task GivenUninitializedHealthStatus_WhenHealthIsChecked_ThenNotHealthyStateIsSaved()
    {
        // health is not initialized
        Task<CustomerKeyHealth> cmkHealthTask = _customerKeyHealthCache.GetAsync();
        Assert.IsTrue(!cmkHealthTask.IsCompleted);

        // check health
        await _validationService.CheckHealth(CancellationToken.None);

        // health has been set, result is returned
        CustomerKeyHealth cmkHealth = await cmkHealthTask;
        Assert.IsTrue(cmkHealth.IsHealthy);
        Assert.IsNull(cmkHealth.Exception);
        Assert.AreEqual(HealthStatusReason.None, cmkHealth.Reason);
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
