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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.CustomerManagedKey.Configs;
using Microsoft.Health.CustomerManagedKey.Health;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Microsoft.Health.CustomerManagedKey.UnitTests;

public class EncryptionHealthCheckTests
{
    private readonly KeyClient _keyClient = Substitute.For<KeyClient>(new Uri("https://keyvault.com"), new DefaultAzureCredential());
    private readonly IKeyTestProvider _keyTestProvider = Substitute.For<IKeyTestProvider>();
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions = new CustomerManagedKeyOptions { KeyName = "test" };

    private readonly EncryptionHealthCheck _healthCheck;

    public EncryptionHealthCheckTests()
    {
        IOptions<CustomerManagedKeyOptions> cmkOptions = Substitute.For<IOptions<CustomerManagedKeyOptions>>();
        cmkOptions.Value.Returns(_customerManagedKeyOptions);

        _keyTestProvider.PerformTestAsync(Arg.Any<KeyClient>(), Arg.Any<CustomerManagedKeyOptions>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        _healthCheck = new EncryptionHealthCheck(
            _keyClient,
            cmkOptions,
            _keyTestProvider,
            NullLogger<EncryptionHealthCheck>.Instance);
    }

    [Fact]
    public async Task GivenBlobDataStoreIsAvailableAndKeyIsAccessible_WhenHealthIsChecked_ThenHealthyStateShouldBeReturned()
    {
        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task GivenKeyAccessFails_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        RequestFailedException requestFailedException = new RequestFailedException("Key is not accessible");
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs(requestFailedException);

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains(DegradedHealthStatusData.CustomerManagedKeyAccessLost.ToString(), result.Data.Keys);
    }

    [Fact]
    public async Task GivenKeyOperationIsInvalid_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs<InvalidOperationException>();

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains(DegradedHealthStatusData.CustomerManagedKeyAccessLost.ToString(), result.Data.Keys);
    }

    [Fact]
    public async Task GivenKeyOperationIsNotSupported_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs<NotSupportedException>();

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains(DegradedHealthStatusData.CustomerManagedKeyAccessLost.ToString(), result.Data.Keys);
    }

    [Fact]
    public async Task GivenKeyWrapUnwrapFails_WhenHealthIsChecked_ThenDegradedHealthIsReturned()
    {
        _keyTestProvider.PerformTestAsync(default, _customerManagedKeyOptions).ThrowsForAnyArgs<CryptographicException>();

        HealthCheckResult result = await _healthCheck.CheckHealthAsync(new HealthCheckContext()).ConfigureAwait(false);
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains(DegradedHealthStatusData.CustomerManagedKeyAccessLost.ToString(), result.Data.Keys);
    }

    [Fact]
    public async Task GivenCancellation_WhenHealthIsChecked_ThenOperationCancelledExceptionIsThrown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationTokenSource.Token)).ConfigureAwait(false);
    }
}
