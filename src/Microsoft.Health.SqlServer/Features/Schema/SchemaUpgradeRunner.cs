// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public class SchemaUpgradeRunner
    {
        private readonly IScriptProvider _scriptProvider;
        private readonly IBaseScriptProvider _baseScriptProvider;
        private readonly IMediator _mediator;
        private readonly ILogger<SchemaUpgradeRunner> _logger;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public SchemaUpgradeRunner(
            IScriptProvider scriptProvider,
            IBaseScriptProvider baseScriptProvider,
            IMediator mediator,
            ILogger<SchemaUpgradeRunner> logger,
            ISqlConnectionFactory sqlConnectionFactory)
        {
            EnsureArg.IsNotNull(scriptProvider, nameof(scriptProvider));
            EnsureArg.IsNotNull(baseScriptProvider, nameof(baseScriptProvider));
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(sqlConnectionFactory, nameof(sqlConnectionFactory));

            _scriptProvider = scriptProvider;
            _baseScriptProvider = baseScriptProvider;
            _mediator = mediator;
            _logger = logger;
            _sqlConnectionFactory = sqlConnectionFactory;
        }

        public async Task ApplySchemaAsync(int version, bool applyFullSchemaSnapshot, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Applying schema {version}", version);

            // Insert schema version as started for diff scripts and 6.sql, 7.sql and so on.
            if (!applyFullSchemaSnapshot || SchemaInformation.DoesNotContainsVersionInSchema(version))
            {
                await InsertSchemaVersionAsync(version, cancellationToken);
            }

            await ExecuteSchemaAsync(_scriptProvider.GetMigrationScript(version, applyFullSchemaSnapshot), cancellationToken);

            await CompleteSchemaVersionAsync(version, cancellationToken);

            _mediator.NotifySchemaUpgradedAsync(version, applyFullSchemaSnapshot).Wait();
            _logger.LogInformation("Completed applying schema {version}", version);
        }

        public async Task ApplyBaseSchemaAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Applying base schema");

            await ExecuteSchemaAsync(_baseScriptProvider.GetScript(), cancellationToken);

            _logger.LogInformation("Completed applying base schema");
        }

        private async Task ExecuteSchemaAsync(string script, CancellationToken cancellationToken)
        {
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);
                var server = new Server(new ServerConnection(connection));

                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }

        private async Task InsertSchemaVersionAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            await UpsertSchemaVersionAsync(schemaVersion, "started", cancellationToken);
        }

        private async Task CompleteSchemaVersionAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            await UpsertSchemaVersionAsync(schemaVersion, "completed", cancellationToken);
        }

        private async Task UpsertSchemaVersionAsync(int schemaVersion, string status, CancellationToken cancellationToken)
        {
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", schemaVersion);
                upsertCommand.Parameters.AddWithValue("@status", status);

                await connection.OpenAsync(cancellationToken);
                await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
