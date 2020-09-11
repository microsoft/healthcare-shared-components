// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Data.SqlClient;
using EnsureThat;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public class SchemaUpgradeRunner
    {
        private readonly IScriptProvider _scriptProvider;
        private readonly IBaseScriptProvider _baseScriptProvider;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly IMediator _mediator;
        private readonly ILogger<SchemaUpgradeRunner> _logger;

        public SchemaUpgradeRunner(
            IScriptProvider scriptProvider,
            IBaseScriptProvider baseScriptProvider,
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
            IMediator mediator,
            ILogger<SchemaUpgradeRunner> logger)
        {
            EnsureArg.IsNotNull(scriptProvider, nameof(scriptProvider));
            EnsureArg.IsNotNull(baseScriptProvider, nameof(baseScriptProvider));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(mediator, nameof(mediator));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _scriptProvider = scriptProvider;
            _baseScriptProvider = baseScriptProvider;
            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _mediator = mediator;
            _logger = logger;
        }

        public void ApplySchema(int version, bool applyFullSchemaSnapshot)
        {
            _logger.LogInformation("Applying schema {version}", version);

            if (!applyFullSchemaSnapshot)
            {
                InsertSchemaVersion(version);
            }

            ExecuteSchema(_scriptProvider.GetMigrationScript(version, applyFullSchemaSnapshot));

            CompleteSchemaVersion(version);

            _mediator.NotifySchemaUpgradedAsync(version, applyFullSchemaSnapshot).Wait();
            _logger.LogInformation("Completed applying schema {version}", version);
        }

        public void ApplyBaseSchema()
        {
            _logger.LogInformation("Applying base schema");

            ExecuteSchema(_baseScriptProvider.GetScript());

            _logger.LogInformation("Completed applying base schema");
        }

        private void ExecuteSchema(string script)
        {
            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                connection.Open();
                var server = new Server(new ServerConnection(connection));

                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }

        private void InsertSchemaVersion(int schemaVersion)
        {
            UpsertSchemaVersion(schemaVersion, "started");
        }

        private void CompleteSchemaVersion(int schemaVersion)
        {
            UpsertSchemaVersion(schemaVersion, "completed");
        }

        private void UpsertSchemaVersion(int schemaVersion, string status)
        {
            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", schemaVersion);
                upsertCommand.Parameters.AddWithValue("@status", status);

                connection.Open();
                upsertCommand.ExecuteNonQuery();
            }
        }
    }
}
