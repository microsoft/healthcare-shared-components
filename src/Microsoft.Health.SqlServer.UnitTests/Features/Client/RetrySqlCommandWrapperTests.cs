// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Extensions;
using Microsoft.Health.SqlServer.Features.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Client
{
    public class RetrySqlCommandWrapperTests
    {
        private static readonly CancellationToken DefaultCancellationToken = new CancellationTokenSource().Token;

        private readonly SqlCommandWrapper _sqlCommandWrapper = Substitute.For<SqlCommandWrapper>(new SqlCommand());
        private readonly IAsyncPolicy _asyncPolicy = Policy
            .Handle<SqlException>(sqlException => sqlException.IsTransient())
            .WaitAndRetryAsync(new TimeSpan[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero });

        private readonly RetrySqlCommandWrapper _retrySqlCommandWrapper;

        public RetrySqlCommandWrapperTests()
        {
            _retrySqlCommandWrapper = new RetrySqlCommandWrapper(_sqlCommandWrapper, _asyncPolicy);
        }

        [Fact]
        public async Task GivenATransientException_WhenNonQueryIsExecuted_ThenItShouldRetry()
        {
            _sqlCommandWrapper.ExecuteNonQueryAsync(DefaultCancellationToken).Throws(CreateTransientException());

            await ExecuteAndValidateExecuteNonQueryAsync(4);
        }

        [Fact]
        public async Task GivenANonTransientException_WhenNonQueryIsExecuted_ThenItShouldNotRetry()
        {
            _sqlCommandWrapper.ExecuteNonQueryAsync(DefaultCancellationToken).Returns<int>(
                _ => throw CreateTransientException(),
                _ => throw CreateNonTransientException());

            await ExecuteAndValidateExecuteNonQueryAsync(2);
        }

        private async Task ExecuteAndValidateExecuteNonQueryAsync(int expectedNumberOfCalls)
        {
            await Assert.ThrowsAsync<SqlException>(() => _retrySqlCommandWrapper.ExecuteNonQueryAsync(DefaultCancellationToken));

            await _sqlCommandWrapper.Received(expectedNumberOfCalls).ExecuteNonQueryAsync(DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenATransientException_WhenReaderIsExecuted_ThenItShouldRetry()
        {
            _sqlCommandWrapper.ExecuteReaderAsync(DefaultCancellationToken).Throws(CreateTransientException());

            await ExecuteAndValidateExecuteReaderAsync(4);
        }

        [Fact]
        public async Task GivenANonTransientException_WhenReaderIsExecuted_ThenItShouldNotRetry()
        {
            _sqlCommandWrapper.ExecuteReaderAsync(DefaultCancellationToken).Returns<SqlDataReader>(
                _ => throw CreateNonTransientException());

            await ExecuteAndValidateExecuteReaderAsync(1);
        }

        private async Task ExecuteAndValidateExecuteReaderAsync(int expectedNumberOfCalls)
        {
            await Assert.ThrowsAsync<SqlException>(() => _retrySqlCommandWrapper.ExecuteReaderAsync(DefaultCancellationToken));

            await _sqlCommandWrapper.Received(expectedNumberOfCalls).ExecuteReaderAsync(DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenATransientException_WhenReaderWithBehaviorIsExecuted_ThenItShouldRetry()
        {
            CommandBehavior behavior = CommandBehavior.SingleRow;

            _sqlCommandWrapper.ExecuteReaderAsync(behavior, DefaultCancellationToken).Throws(CreateTransientException());

            await ExecuteAndValidateExecuteReaderAsync(behavior, 4);
        }

        [Fact]
        public async Task GivenANonTransientException_WhenReaderWithBehaviorIsExecuted_ThenItShouldNotRetry()
        {
            CommandBehavior behavior = CommandBehavior.SchemaOnly;

            _sqlCommandWrapper.ExecuteReaderAsync(behavior, DefaultCancellationToken).Returns<SqlDataReader>(
                _ => throw CreateNonTransientException());

            await ExecuteAndValidateExecuteReaderAsync(behavior, 1);
        }

        private async Task ExecuteAndValidateExecuteReaderAsync(CommandBehavior behavior, int expectedNumberOfCalls)
        {
            await Assert.ThrowsAsync<SqlException>(() => _retrySqlCommandWrapper.ExecuteReaderAsync(behavior, DefaultCancellationToken));

            await _sqlCommandWrapper.Received(expectedNumberOfCalls).ExecuteReaderAsync(behavior, DefaultCancellationToken);
        }

        [Fact]
        public async Task GivenATransientException_WhenScalarIsExecuted_ThenItShouldRetry()
        {
            _sqlCommandWrapper.ExecuteScalarAsync(DefaultCancellationToken).Throws(CreateTransientException());

            await ExecuteAndValidateExecuteScalarAsync(4);
        }

        [Fact]
        public async Task GivenANonTransientException_WhenScalarIsExecuted_ThenItShouldNotRetry()
        {
            _sqlCommandWrapper.ExecuteScalarAsync(DefaultCancellationToken).Returns(
                _ => throw CreateTransientException(),
                _ => throw CreateTransientException(),
                _ => throw CreateNonTransientException());

            await ExecuteAndValidateExecuteScalarAsync(3);
        }

        private async Task ExecuteAndValidateExecuteScalarAsync(int expectedNumberOfCalls)
        {
            await Assert.ThrowsAsync<SqlException>(() => _retrySqlCommandWrapper.ExecuteScalarAsync(DefaultCancellationToken));

            await _sqlCommandWrapper.Received(expectedNumberOfCalls).ExecuteScalarAsync(DefaultCancellationToken);
        }

        private static SqlException CreateTransientException()
            => SqlExceptionFactory.Create(10928);

        private static SqlException CreateNonTransientException()
            => SqlExceptionFactory.Create(50404);
    }
}
