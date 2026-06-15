// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Health;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Health;
using Microsoft.Health.SqlServer.Features.Storage;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Health;

public sealed class SqlServerHealthCheckTests
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly SqlTransactionHandler _sqlTransactionHandler;
    private readonly ISqlConnectionBuilder _sqlConnectionBuilder;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private readonly IOptions<SqlServerDataStoreConfiguration> _sqlServerDataStoreConfiguration;
    private readonly ValueCache<CustomerKeyHealth> _cache;

    public SqlServerHealthCheckTests()
    {
        _logger = Substitute.For<ILogger<SqlServerHealthCheck>>();
        _sqlTransactionHandler = Substitute.For<SqlTransactionHandler>();
        _sqlConnectionBuilder = Substitute.For<ISqlConnectionBuilder>();
        _sqlRetryLogicBaseProvider = Substitute.For<SqlRetryLogicBaseProvider>();

        _sqlServerDataStoreConfiguration = Substitute.For<IOptions<SqlServerDataStoreConfiguration>>();
        _sqlServerDataStoreConfiguration.Value.Returns(new SqlServerDataStoreConfiguration());

        _cache = new ValueCache<CustomerKeyHealth>();
        _cache.Set(new CustomerKeyHealth() { IsHealthy = true });
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task GivenASqlHealthCheck_WhenSqlConnectionWrapperThrowsAnInvalidAccess_ThenHandlesItProperlyAsDegraded(HttpStatusCode statusCode)
    {
        HealthCheckResult healthCheckResult = await GetHealthCheckResultGivenAnErrorHttpStatusCodeAsync(statusCode);

        Assert.Equal(HealthStatus.Degraded, healthCheckResult.Status);
        Assert.Equal(HealthStatusReason.DataStoreConnectionDegraded.ToString(), healthCheckResult.Data["Reason"]);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GivenASqlHealthCheck_WhenSqlConnectionWrapperThrowsAnUnknownStatusCode_ThenHandlesItError(HttpStatusCode statusCode)
    {
        HttpRequestException httpException = await Assert.ThrowsAsync<HttpRequestException>(() => GetHealthCheckResultGivenAnErrorHttpStatusCodeAsync(statusCode));

        Assert.Equal(statusCode, httpException.StatusCode);
    }

    private async Task<HealthCheckResult> GetHealthCheckResultGivenAnErrorHttpStatusCodeAsync(HttpStatusCode statusCode)
    {
        // Exception thrown by the SqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync method.
        var httpRequestException = new HttpRequestException("error", inner: null, statusCode: statusCode);

        SqlConnectionWrapperFactory connectionWrapperFactory = Substitute.For<SqlConnectionWrapperFactory>(
            _sqlTransactionHandler,
            _sqlConnectionBuilder,
            _sqlRetryLogicBaseProvider,
            _sqlServerDataStoreConfiguration);

        // Setting up the ObtainSqlConnectionWrapperAsync method to throw an exception.
        connectionWrapperFactory.ObtainSqlConnectionWrapperAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<SqlConnectionWrapper>(httpRequestException));

        var sqlHealthCheck = new SqlServerHealthCheck(connectionWrapperFactory, _cache, _sqlServerDataStoreConfiguration, _logger);

        return await sqlHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
    }

    [Fact]
    public async Task GivenASqlHealthCheck_WhenSqlConnectionWrapperThrowsLoginFailure_ThenReturnsUnhealthyWithException()
    {
        // SQL error 18456 = "Login failed for user" — what happens when the database is deleted.
        SqlException loginFailure = SqlExceptionFactory.Create(18456, "Login failed for user 'svc'. The database does not exist.");

        SqlConnectionWrapperFactory connectionWrapperFactory = Substitute.For<SqlConnectionWrapperFactory>(
            _sqlTransactionHandler,
            _sqlConnectionBuilder,
            _sqlRetryLogicBaseProvider,
            _sqlServerDataStoreConfiguration);

        connectionWrapperFactory.ObtainSqlConnectionWrapperAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SqlConnectionWrapper>(loginFailure));

        var sqlHealthCheck = new SqlServerHealthCheck(connectionWrapperFactory, _cache, _sqlServerDataStoreConfiguration, _logger);

        HealthCheckResult result = await sqlHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Same(loginFailure, result.Exception);
        Assert.Equal(HealthStatusReason.DataStoreConnectionDegraded.ToString(), result.Data["Reason"]);
    }

    [Fact]
    public async Task GivenASqlHealthCheck_WhenProbeExceedsConfiguredTimeout_ThenReturnsUnhealthy()
    {
        var configWithShortTimeout = new SqlServerDataStoreConfiguration
        {
            HealthCheckProbeTimeout = TimeSpan.FromMilliseconds(50),
        };
        IOptions<SqlServerDataStoreConfiguration> options = Substitute.For<IOptions<SqlServerDataStoreConfiguration>>();
        options.Value.Returns(configWithShortTimeout);

        SqlConnectionWrapperFactory connectionWrapperFactory = Substitute.For<SqlConnectionWrapperFactory>(
            _sqlTransactionHandler,
            _sqlConnectionBuilder,
            _sqlRetryLogicBaseProvider,
            _sqlServerDataStoreConfiguration);

        // Simulate the SQL wrapper hanging until its token is cancelled (the probe-internal CTS will trip).
        connectionWrapperFactory.ObtainSqlConnectionWrapperAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                CancellationToken token = callInfo.Arg<CancellationToken>();
                await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                return null;
            });

        var sqlHealthCheck = new SqlServerHealthCheck(connectionWrapperFactory, _cache, options, _logger);

        HealthCheckResult result = await sqlHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("timeout", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HealthStatusReason.DataStoreConnectionDegraded.ToString(), result.Data["Reason"]);
    }

    [Fact]
    public async Task GivenASqlHealthCheck_WhenCallerCancels_ThenOperationCanceledExceptionPropagates()
    {
        // When the framework's outer token cancels (the caller), we must NOT swallow it — the
        // framework's hosted service relies on OCE propagation to distinguish "publisher cancelled
        // the batch" from a real health failure.
        SqlConnectionWrapperFactory connectionWrapperFactory = Substitute.For<SqlConnectionWrapperFactory>(
            _sqlTransactionHandler,
            _sqlConnectionBuilder,
            _sqlRetryLogicBaseProvider,
            _sqlServerDataStoreConfiguration);

        connectionWrapperFactory.ObtainSqlConnectionWrapperAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                CancellationToken token = callInfo.Arg<CancellationToken>();
                await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                return null;
            });

        var sqlHealthCheck = new SqlServerHealthCheck(connectionWrapperFactory, _cache, _sqlServerDataStoreConfiguration, _logger);

        using var callerCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sqlHealthCheck.CheckHealthAsync(new HealthCheckContext(), callerCts.Token));
    }
}
