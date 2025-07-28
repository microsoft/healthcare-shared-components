// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Health;
using Xunit;

namespace Microsoft.Health.Encryption.UnitTests;

public class CustomerManagedKeyHealthCacheTests
{
    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        // Act
        var instance1 = CustomerManagedKeyHealthCache.Instance;
        var instance2 = CustomerManagedKeyHealthCache.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public async Task SetAndGet_ShouldReturnSameValue()
    {
        // Arrange
        var health = new CustomerKeyHealth
        {
            IsHealthy = false,
            Reason = HealthStatusReason.ServiceUnavailable,
            Exception = new InvalidOperationException("Test exception")
        };

        // Act
        CustomerManagedKeyHealthCache.Instance.Set(health);

        // Assert
        var cachedHealth = await CustomerManagedKeyHealthCache.Instance.GetAsync();
        Assert.False(cachedHealth.IsHealthy);
        Assert.Equal(HealthStatusReason.ServiceUnavailable, cachedHealth.Reason);
        Assert.IsType<InvalidOperationException>(cachedHealth.Exception);
    }

    [Fact]
    public async Task Set_ShouldOverwritePreviousValue()
    {
        // Arrange
        var health1 = new CustomerKeyHealth { IsHealthy = true, Reason = HealthStatusReason.None };
        var health2 = new CustomerKeyHealth { IsHealthy = false, Reason = HealthStatusReason.CustomerManagedKeyAccessLost };

        // Act
        CustomerManagedKeyHealthCache.Instance.Set(health1);
        var cachedHealth1 = await CustomerManagedKeyHealthCache.Instance.GetAsync();

        CustomerManagedKeyHealthCache.Instance.Set(health2);
        var cachedHealth2 = await CustomerManagedKeyHealthCache.Instance.GetAsync();

        // Assert
        Assert.True(cachedHealth1.IsHealthy);
        Assert.Equal(HealthStatusReason.None, cachedHealth1.Reason);

        Assert.False(cachedHealth2.IsHealthy);
        Assert.Equal(HealthStatusReason.CustomerManagedKeyAccessLost, cachedHealth2.Reason);
    }
}
