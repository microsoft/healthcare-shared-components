// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.SqlServer.Features.Health
{
    /// <summary>
    /// An <see cref="IHealthCheck"/> implementation that verifies connectivity to the SQL database
    /// </summary>
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly ILogger<SqlServerHealthCheck> _logger;
        private readonly ISqlConnection _sqlConnection;

        public SqlServerHealthCheck(ISqlConnection sqlConnection, ILogger<SqlServerHealthCheck> logger)
        {
            EnsureArg.IsNotNull(sqlConnection, nameof(sqlConnection));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnection = sqlConnection;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                using SqlConnection connection = await _sqlConnection.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                using SqlCommand command = connection.CreateCommand();
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                command.CommandText = "select @@DBTS";

                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

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
