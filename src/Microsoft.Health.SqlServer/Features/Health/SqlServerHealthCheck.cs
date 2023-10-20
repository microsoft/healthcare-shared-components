// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Encryption.Customer.Health;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Features.Health;

/// <summary>
/// An <see cref="IHealthCheck"/> implementation that verifies connectivity to the SQL database
/// </summary>
public class SqlServerHealthCheck : KeyAccessAndDataStoreStateDependent, IHealthCheck
{
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

        if (IsImpactedByCustomerKeyHealth(cmkStatus))
        {
            // if the customer-managed key is inaccessible, storage will also be inaccessible
            return new HealthCheckResult(
                HealthStatus.Degraded,
                DegradedDescription,
                cmkStatus.Exception,
                new Dictionary<string, object> { { "Reason", cmkStatus.Reason } });
        }

        using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        sqlCommandWrapper.CommandText = "select @@DBTS";

        await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully connected to SQL database.");
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
