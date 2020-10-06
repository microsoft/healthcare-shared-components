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
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer.Features.Health
{
    /// <summary>
    /// An <see cref="IHealthCheck"/> implementation that verifies connectivity to the SQL database
    /// </summary>
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly SqlServerDataStoreConfiguration _configuration;
        private readonly ILogger<SqlServerHealthCheck> _logger;
        private readonly ISqlConnectionFactory _sqlConnection;

        public SqlServerHealthCheck(SqlServerDataStoreConfiguration configuration, ISqlConnectionFactory sqlConnection, ILogger<SqlServerHealthCheck> logger)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(sqlConnection, nameof(sqlConnection));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _configuration = configuration;
            _sqlConnection = sqlConnection;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                using (var connection = _sqlConnection.GetSqlConnection())
                using (SqlCommand command = connection.CreateCommand())
                {
                    await connection.OpenAsync(cancellationToken);

                    command.CommandText = "select @@DBTS";

                    await command.ExecuteScalarAsync(cancellationToken);

                    return HealthCheckResult.Healthy("Successfully connected to the data store.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to the data store.");
                return HealthCheckResult.Unhealthy("Failed to connect to the data store.");
            }
        }
    }
}
