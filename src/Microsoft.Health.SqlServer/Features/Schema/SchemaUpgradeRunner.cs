﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Data.SqlClient;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public class SchemaUpgradeRunner
    {
        private readonly IScriptProvider _scriptProvider;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly ILogger<SchemaUpgradeRunner> _logger;

        public SchemaUpgradeRunner(IScriptProvider scriptProvider, SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration, ILogger<SchemaUpgradeRunner> logger)
        {
            EnsureArg.IsNotNull(scriptProvider, nameof(scriptProvider));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _scriptProvider = scriptProvider;
            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _logger = logger;
        }

        public void ApplySchema(int version, bool applyFullSchemaSnapshot)
        {
            _logger.LogInformation("Applying schema {version}", version);

            if (!applyFullSchemaSnapshot)
            {
                InsertSchemaVersion(version);
            }

            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                connection.Open();
                var server = new Server(new ServerConnection(connection));

                server.ConnectionContext.ExecuteNonQuery(_scriptProvider.GetMigrationScript(version, applyFullSchemaSnapshot));
            }

            CompleteSchemaVersion(version);

            _logger.LogInformation("Completed applying schema {version}", version);
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
