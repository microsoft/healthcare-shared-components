// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
    private readonly IOptionsMonitor<SqlServerDataStoreConfiguration> _sqlServerDataStoreConfiguration;
    private readonly ValueCache<CustomerKeyHealth> _cache;

    public SqlServerHealthCheckTests()
    {
        _logger = Substitute.For<ILogger<SqlServerHealthCheck>>();
        _sqlTransactionHandler = Substitute.For<SqlTransactionHandler>();
        _sqlConnectionBuilder = Substitute.For<ISqlConnectionBuilder>();
        _sqlRetryLogicBaseProvider = Substitute.For<SqlRetryLogicBaseProvider>();

        _sqlServerDataStoreConfiguration = Substitute.For<IOptionsMonitor<SqlServerDataStoreConfiguration>>();
        _sqlServerDataStoreConfiguration.Get(Arg.Any<string>()).Returns(new SqlServerDataStoreConfiguration());

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

        var sqlHealthCheck = new SqlServerHealthCheck(connectionWrapperFactory, _cache, _logger);

        return await sqlHealthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
    }
}
