// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
public class SqlServerHealthCheck : StorageHealthCheck
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;

    public SqlServerHealthCheck(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
        ValueCache<CustomerKeyHealth> customerKeyHealthCache,
        ILogger<SqlServerHealthCheck> logger)
        : base(customerKeyHealthCache, logger)
    {
        _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public override async Task<HealthCheckResult> CheckStorageHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Performing health check for {nameof(SqlServerHealthCheck)}");

            using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
            using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

            sqlCommandWrapper.CommandText = "select @@DBTS";

            await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully connected to SQL database.");

            return HealthCheckResult.Healthy("Successfully connected.");
        }
        // Filter on error codes for azure key vault https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors-31000-to-41399?view=sql-server-ver16
        catch (SqlException e) when (e.Number is 40981 or 33183 or 33184 or 40925)
        {
            // Error 40925: "Can not connect to the database in its current state". This error can be for various DB states (recovering, inacessible) but we assume that our DB will only hit this for Inaccessible state
            HealthStatusReason reason = e.Number is 40925
                ? HealthStatusReason.DataStoreStateDegraded
                : HealthStatusReason.CustomerManagedKeyAccessLost;

            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                e,
                new Dictionary<string, object> { { "Reason", reason } });
        }
    }
}
