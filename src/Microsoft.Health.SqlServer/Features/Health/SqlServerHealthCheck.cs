// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Health;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Features.Health;

/// <summary>
/// An <see cref="IHealthCheck"/> implementation that verifies connectivity to the SQL database
/// </summary>
public class SqlServerHealthCheck : IHealthCheck
{
    // This health check will not pass if the cached health is failing for any of these reasons
    private readonly IEnumerable<HealthStatusReason> _dependentHealthStatusReasons = new List<HealthStatusReason> { HealthStatusReason.CustomerManagedKeyAccessLost, HealthStatusReason.DataStoreStateDegraded };
    private const string DegradedDescription = "The health of the store has degraded.";

    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
    private readonly ValueCache<CustomerKeyHealth> _customerKeyHealthCache;

    public SqlServerHealthCheck(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        ILogger<SqlServerHealthCheck> logger)
    {
        _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        _customerKeyHealthCache = EnsureArg.IsNotNull(customerKeyHealthCache, nameof(customerKeyHealthCache));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting {nameof(SqlServerHealthCheck)}.");

        CustomerKeyHealth cmkStatus = await _customerKeyHealthCache.GetAsync(cancellationToken).ConfigureAwait(false);

        if (!cmkStatus.IsHealthy &&
            _dependentHealthStatusReasons.Contains(cmkStatus.Reason))
        {
            // if the customer-managed key is inaccessible, storage will also be inaccessible
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                cmkStatus.Exception,
                new Dictionary<string, object> { { "Reason", cmkStatus.Reason } });
        }

        try
        {
            using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

            sqlCommandWrapper.CommandText = "select @@DBTS";

            await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully connected to SQL database.");
            return HealthCheckResult.Healthy("Successfully connected.");
        }
        // Error: Can not connect to the database in its current state. This error can be for various DB states (recovering, inacessible) but we assume that our DB will only hit this for Inaccessible state
        catch (SqlException ex) when (ex.ErrorCode == 40925)
        {
            // DB is status in Inaccessible because the encryption key was inacessible for > 30 mins. User must reprovision or we need to revalidate key on SQL DB. 
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                ex,
                new Dictionary<string, object> { { "Reason", HealthStatusReason.DataStoreStateDegraded } });
        }
    }
}
