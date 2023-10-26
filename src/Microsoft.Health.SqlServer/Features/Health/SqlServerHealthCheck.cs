// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
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

    public override string DegradedDescription => "The health of the store has degraded.";

    public override Func<Exception, bool> CMKAccessLostExceptionFilter => CustomerKeyConstants.SQLExceptionFilter;

    public override async Task CheckStorageHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Performing health check for {nameof(SqlServerHealthCheck)}");

        using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        sqlCommandWrapper.CommandText = "select @@DBTS";

        await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully connected to SQL database.");
    }
}
