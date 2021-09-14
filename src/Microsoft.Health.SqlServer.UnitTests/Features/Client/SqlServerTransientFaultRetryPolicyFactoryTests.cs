// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using NSubstitute;
using Polly;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Client
{
    public class SqlServerTransientFaultRetryPolicyFactoryTests
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
        private readonly IPollyRetryLoggerFactory _pollyRetryLoggerFactory = Substitute.For<IPollyRetryLoggerFactory>();

        private readonly SqlServerTransientFaultRetryPolicyFactory _sqlServerTransientFaultRetryPolicyFactory;

        private readonly IAsyncPolicy _asyncPolicy;
        private readonly List<TimeSpan> _capturedRetries = new List<TimeSpan>();

        public SqlServerTransientFaultRetryPolicyFactoryTests()
        {
            _sqlServerDataStoreConfiguration.TransientFaultRetryPolicy = new SqlServerTransientFaultRetryPolicyConfiguration()
            {
                InitialDelay = TimeSpan.FromMilliseconds(200),
                RetryCount = 3,
                Factor = 3,
                FastFirst = true,
            };

            Action<Exception, TimeSpan, int, Context> onRetryCapture = (exception, sleepDuration, retryCount, _) =>
            {
                _capturedRetries.Add(sleepDuration);
            };

            _pollyRetryLoggerFactory.Create().Returns(onRetryCapture);

            _sqlServerTransientFaultRetryPolicyFactory = new SqlServerTransientFaultRetryPolicyFactory(
                Options.Create(_sqlServerDataStoreConfiguration),
                _pollyRetryLoggerFactory);

            _asyncPolicy = _sqlServerTransientFaultRetryPolicyFactory.Create();
        }

        [Fact]
        public async Task GivenATransientException_WhenRetryPolicyIsUsed_ThenItShouldRetry()
        {
            await Assert.ThrowsAsync<SqlException>(() =>
                _asyncPolicy.ExecuteAsync(() => Task.Run(() => throw SqlExceptionFactory.CreateTransientException())));

            ValidateCapturedRetries();
        }

        [Fact]
        public async Task GivenANonTransientException_WhenRetryPolicyIsUsed_ThenItShouldNotRetry()
        {
            await Assert.ThrowsAsync<SqlException>(() =>
                _asyncPolicy.ExecuteAsync(() => Task.Run(() => throw SqlExceptionFactory.CreateNonTransientException())));

            Assert.Empty(_capturedRetries);
        }

        [Fact]
        public async Task GivenADeadlockedException_WhenRetryPolicyIsUsed_ThenItShouldRetry()
        {
            await Assert.ThrowsAsync<SqlException>(() =>
                _asyncPolicy.ExecuteAsync(() => Task.Run(() => throw SqlExceptionFactory.CreateDeadlockException())));

            ValidateCapturedRetries();
        }

        [Fact]
        public async Task GivenATimeoutException_WhenRetryPolicyIsUsed_ThenItShouldRetry()
        {
            await Assert.ThrowsAsync<TimeoutException>(() =>
                _asyncPolicy.ExecuteAsync(() => Task.Run(() => throw new TimeoutException())));

            ValidateCapturedRetries();
        }

        [Fact]
        public async Task GivenOtherException_WhenRetryPolicyIsUsed_ThenItShouldNotRetry()
        {
            await Assert.ThrowsAsync<Exception>(() =>
                _asyncPolicy.ExecuteAsync(() => Task.Run(() => throw new Exception())));

            Assert.Empty(_capturedRetries);
        }

        private void ValidateCapturedRetries()
        {
            Assert.Collection(
                _capturedRetries,
                item => Assert.Equal(TimeSpan.Zero, item),
                item => Assert.Equal(TimeSpan.FromMilliseconds(200), item),
                item => Assert.Equal(TimeSpan.FromMilliseconds(600), item));
        }
    }
}
