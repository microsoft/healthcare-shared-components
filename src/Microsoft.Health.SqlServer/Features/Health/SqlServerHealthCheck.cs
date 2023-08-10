// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Extensions;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Features.Health;

/// <summary>
/// An <see cref="IHealthCheck"/> implementation that verifies connectivity to the SQL database
/// </summary>
public class SqlServerHealthCheck : IHealthCheck
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
    private readonly IStoragePrerequisiteHealthReport _storagePrerequisiteHealthReport;

    public SqlServerHealthCheck(SqlConnectionWrapperFactory sqlConnectionWrapperFactory, IStoragePrerequisiteHealthReport storagePrerequisiteHealthReport, ILogger<SqlServerHealthCheck> logger)
    {
        _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        _storagePrerequisiteHealthReport = EnsureArg.IsNotNull(storagePrerequisiteHealthReport, nameof(storagePrerequisiteHealthReport));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting {nameof(SqlServerHealthCheck)}.");

        if (_storagePrerequisiteHealthReport.HealthReport != null && _storagePrerequisiteHealthReport.HealthReport.Status != HealthStatus.Healthy)
        {
            // If the prerequisite checks are unhealthy, do not check storage and return the lowest status
            HealthReportEntry reportEntryWithLowestStatus = _storagePrerequisiteHealthReport.HealthReport.FindLowestHealthReportEntry();

            return new HealthCheckResult(
                _storagePrerequisiteHealthReport.HealthReport.Status,
                reportEntryWithLowestStatus.Description,
                reportEntryWithLowestStatus.Exception,
                reportEntryWithLowestStatus.Data);
        }

        using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
        using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand();

        sqlCommandWrapper.CommandText = "select @@DBTS";

        await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully connected to SQL database.");
        return HealthCheckResult.Healthy("Successfully connected.");
    }
}
