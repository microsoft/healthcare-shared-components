﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Features.Health
{
    /// <summary>
    /// An <see cref="IHealthCheck"/> implementation that verifies connectivity to the SQL database
    /// </summary>
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly ILogger<SqlServerHealthCheck> _logger;
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly RetrySqlAction _retrySqlAction;

        public SqlServerHealthCheck(
                                    SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
                                    RetrySqlAction retrySqlAction,
                                    ILogger<SqlServerHealthCheck> logger)
        {
            _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            _retrySqlAction = EnsureArg.IsNotNull(retrySqlAction, nameof(retrySqlAction));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                await _retrySqlAction.ExecuteAsync(async () =>
                {
                    using SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken);
                    using SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateSqlCommand();

                    sqlCommandWrapper.CommandText = "select @@DBTS";

                    await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                });
                return HealthCheckResult.Healthy("Successfully connected.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to the data store.");

                return HealthCheckResult.Unhealthy("Failed to connect.");
            }
        }
    }
}
