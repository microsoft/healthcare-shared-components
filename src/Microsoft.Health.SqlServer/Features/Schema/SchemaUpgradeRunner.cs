// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
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

        public void ApplySchema(int version, bool applyFullSchemaSnapshot)
        {
            _logger.LogInformation("Applying schema {version}", version);

            if (!applyFullSchemaSnapshot)
            {
                InsertSchemaVersion(version);
            }

            ExecuteSchemaAsync(_scriptProvider.GetMigrationScript(version, applyFullSchemaSnapshot));

            CompleteSchemaVersion(version);

            _mediator.NotifySchemaUpgradedAsync(version).Wait();
            _logger.LogInformation("Completed applying schema {version}", version);
        }

        public void ApplyBaseSchema()
        {
            _logger.LogInformation("Applying base schema");

            ExecuteSchemaAsync(_baseScriptProvider.GetScript());

            _logger.LogInformation("Completed applying base schema");
        }

        private async void ExecuteSchemaAsync(string script)
        {
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync())
            {
                await connection.OpenAsync();
                var server = new Server(new ServerConnection(connection));

                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }

        private void InsertSchemaVersion(int schemaVersion)
        {
            UpsertSchemaVersionAsync(schemaVersion, "started");
        }

        private void CompleteSchemaVersion(int schemaVersion)
        {
            UpsertSchemaVersionAsync(schemaVersion, "completed");
        }

        private async void UpsertSchemaVersionAsync(int schemaVersion, string status)
        {
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync())
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", schemaVersion);
                upsertCommand.Parameters.AddWithValue("@status", status);

                await connection.OpenAsync();
                await upsertCommand.ExecuteNonQueryAsync();
            }
        }
    }
}
