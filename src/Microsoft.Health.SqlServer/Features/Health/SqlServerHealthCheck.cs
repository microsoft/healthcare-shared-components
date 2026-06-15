// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Health;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Health;

/// <summary>
/// An <see cref="IHealthCheck"/> implementation that verifies connectivity to the SQL database.
/// </summary>
/// <remarks>
/// The probe is bounded by <see cref="SqlServerDataStoreConfiguration.HealthCheckProbeTimeout"/> so
/// that a deleted/unreachable database (which can otherwise exhaust the SqlClient connect-timeout
/// plus retry budget for ~70s) cannot consume the full
/// <c>HealthCheckPublisherOptions.Timeout</c> and cause the publisher to skip publishing the
/// resulting failure. When the probe times out or any <see cref="SqlException"/> escapes, the
/// check returns <see cref="HealthStatus.Unhealthy"/> with the underlying exception attached so
/// the published <see cref="HealthReport"/> contains real diagnostics.
/// </remarks>
public class SqlServerHealthCheck : StorageHealthCheck
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
    private readonly TimeSpan _probeTimeout;

    public SqlServerHealthCheck(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration,
        ILogger<SqlServerHealthCheck> logger)
        : base(customerKeyHealthCache, logger)
    {
        _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
        _probeTimeout = sqlServerDataStoreConfiguration.Value.HealthCheckProbeTimeout;
    }

    public override async Task<HealthCheckResult> CheckStorageHealthAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_probeTimeout > TimeSpan.Zero && _probeTimeout != Timeout.InfiniteTimeSpan)
        {
            probeCts.CancelAfter(_probeTimeout);
        }

        try
        {
            _logger.LogInformation($"Performing health check for {nameof(SqlServerHealthCheck)}");

            using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(probeCts.Token).ConfigureAwait(false);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

            sqlCommandWrapper.CommandText = "select @@DBTS";

            await sqlCommandWrapper.ExecuteScalarAsync(probeCts.Token).ConfigureAwait(false);

            _logger.LogInformation("Successfully connected to SQL database.");

            return HealthCheckResult.Healthy("Successfully connected.");
        }
        catch (HttpRequestException httpEx) when (httpEx.IsInvalidAccess())
        {
            // Attempts to retrieve the connection string can fail with HTTP errors if the SQL Connection Wrapper relies on
            // HTTP requests. For this reason, these HTTP errors must be caught and properly handled.

            HealthStatusReason reason = HealthStatusReason.DataStoreConnectionDegraded;

            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                httpEx,
                new Dictionary<string, object> { { "Reason", reason.ToString() } });
        }
        catch (SqlException sqlEx) when (sqlEx.IsCMKError())
        {
            // Error 40925: "Can not connect to the database in its current state". This error can be for various DB states (recovering, inacessible) but we assume that our DB will only hit this for Inaccessible state
            HealthStatusReason reason = sqlEx.Number is SqlErrorCodes.CannotConnectToDBInCurrentState
                ? HealthStatusReason.DataStoreStateDegraded
                : HealthStatusReason.CustomerManagedKeyAccessLost;

            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                sqlEx,
                new Dictionary<string, object> { { "Reason", reason.ToString() } });
        }
        catch (OperationCanceledException) when (probeCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // The probe exceeded its per-probe budget (the caller did not cancel). Surface this as
            // Unhealthy so the publisher can record and ship a real failure instead of waiting for
            // the framework's outer timeout to fire (which would skip publishing entirely).
            _logger.LogWarning("SQL health check probe exceeded its timeout of {ProbeTimeoutSeconds}s.", _probeTimeout.TotalSeconds);

            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                description: $"SQL health check probe exceeded its timeout of {_probeTimeout.TotalSeconds}s.",
                exception: null,
                data: new Dictionary<string, object> { { "Reason", HealthStatusReason.DataStoreConnectionDegraded.ToString() } });
        }
        catch (SqlException sqlEx)
        {
            // Any other SqlException (login failure on deleted DB, connection refused, timeout, etc.).
            // Convert to Unhealthy explicitly so the publisher gets a real Unhealthy entry with the
            // underlying exception attached. Without this, the exception would propagate out of
            // CheckHealthAsync and be either (a) converted by the framework into Unhealthy (fine, but
            // only if cancellation has not been requested) or (b) lost entirely if the publisher's
            // outer token has already cancelled.
            _logger.LogWarning(sqlEx, "SQL health check failed with SqlException (Number={SqlErrorNumber}).", sqlEx.Number);

            return new HealthCheckResult(
                HealthStatus.Unhealthy,
                description: "Failed to connect to SQL database.",
                exception: sqlEx,
                data: new Dictionary<string, object> { { "Reason", HealthStatusReason.DataStoreConnectionDegraded.ToString() } });
        }
    }
}
