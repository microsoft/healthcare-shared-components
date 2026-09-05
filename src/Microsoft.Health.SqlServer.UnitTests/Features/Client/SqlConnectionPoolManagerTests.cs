// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Client;

public sealed class SqlConnectionPoolManagerTests
{
    private readonly ISqlConnectionPoolResetter _poolResetter;
    private readonly ILogger<SqlConnectionPoolManager> _logger;
    private readonly SqlConnectionPoolManager _poolManager;

    public SqlConnectionPoolManagerTests()
    {
        _poolResetter = Substitute.For<ISqlConnectionPoolResetter>();
        _logger = Substitute.For<ILogger<SqlConnectionPoolManager>>();
        _poolManager = new SqlConnectionPoolManager(_poolResetter, _logger, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(SqlErrorCodes.TransportLevelError)]
    [InlineData(SqlErrorCodes.SemaphoreTimeout)]
    [InlineData(SqlErrorCodes.ConnectionForciblyClosedByRemoteHost)]
    public void HandleError_TransportError_ClearsPoolAndReturnsTrue(int errorCode)
    {
        SqlException exception = SqlExceptionFactory.Create(errorCode);

        bool result = _poolManager.HandleError(exception);

        Assert.True(result);
        _poolResetter.Received(1).ClearAllPools();
    }

    [Fact]
    public void HandleError_NonTransportError_DoesNotClearPoolAndReturnsFalse()
    {
        SqlException exception = SqlExceptionFactory.Create(18456); // Login failed

        bool result = _poolManager.HandleError(exception);

        Assert.False(result);
        _poolResetter.DidNotReceive().ClearAllPools();
    }

    [Fact]
    public void HandleError_NullException_ReturnsFalse()
    {
        bool result = _poolManager.HandleError(null);

        Assert.False(result);
        _poolResetter.DidNotReceive().ClearAllPools();
    }

    [Fact]
    public void HandleError_SecondCallWithinCooldown_DoesNotClearPoolAgain()
    {
        SqlException exception = SqlExceptionFactory.Create(SqlErrorCodes.TransportLevelError);

        bool firstResult = _poolManager.HandleError(exception);
        bool secondResult = _poolManager.HandleError(exception);

        Assert.True(firstResult);
        Assert.False(secondResult);
        _poolResetter.Received(1).ClearAllPools();
    }

    [Fact]
    public void HandleError_AfterCooldownExpires_ClearsPoolAgain()
    {
        var shortCooldown = TimeSpan.FromMilliseconds(50);
        var manager = new SqlConnectionPoolManager(_poolResetter, _logger, shortCooldown);
        SqlException exception = SqlExceptionFactory.Create(SqlErrorCodes.TransportLevelError);

        bool firstResult = manager.HandleError(exception);
        Assert.True(firstResult);

        Thread.Sleep(100); // Wait for cooldown to expire

        bool secondResult = manager.HandleError(exception);
        Assert.True(secondResult);

        _poolResetter.Received(2).ClearAllPools();
    }

    [Theory]
    [InlineData(SqlErrorCodes.TransportLevelError)]
    [InlineData(SqlErrorCodes.SemaphoreTimeout)]
    [InlineData(SqlErrorCodes.ConnectionForciblyClosedByRemoteHost)]
    public void IsTransportError_WithTransportErrorCodes_ReturnsTrue(int errorCode)
    {
        SqlException exception = SqlExceptionFactory.Create(errorCode);

        Assert.True(SqlConnectionPoolManager.IsTransportError(exception));
    }

    [Theory]
    [InlineData(18456)] // Login failed
    [InlineData(1205)]  // Deadlock
    [InlineData(49918)] // Not enough resources
    public void IsTransportError_WithNonTransportErrorCodes_ReturnsFalse(int errorCode)
    {
        SqlException exception = SqlExceptionFactory.Create(errorCode);

        Assert.False(SqlConnectionPoolManager.IsTransportError(exception));
    }
}
