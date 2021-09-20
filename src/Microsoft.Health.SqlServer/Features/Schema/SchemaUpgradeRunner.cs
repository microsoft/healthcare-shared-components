// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public class SchemaUpgradeRunner
    {
        private readonly IScriptProvider _scriptProvider;
        private readonly IBaseScriptProvider _baseScriptProvider;
        private readonly ILogger<SchemaUpgradeRunner> _logger;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;
        private ISchemaManagerDataStore _schemaManagerDataStore;

        public SchemaUpgradeRunner(
            IScriptProvider scriptProvider,
            IBaseScriptProvider baseScriptProvider,
            ILogger<SchemaUpgradeRunner> logger,
            ISqlConnectionFactory sqlConnectionFactory,
            ISchemaManagerDataStore schemaManagerDataStore)
        {
            EnsureArg.IsNotNull(scriptProvider, nameof(scriptProvider));
            EnsureArg.IsNotNull(baseScriptProvider, nameof(baseScriptProvider));
            EnsureArg.IsNotNull(logger, nameof(logger));
            EnsureArg.IsNotNull(sqlConnectionFactory, nameof(sqlConnectionFactory));
            EnsureArg.IsNotNull(schemaManagerDataStore, nameof(schemaManagerDataStore));

            _scriptProvider = scriptProvider;
            _baseScriptProvider = baseScriptProvider;
            _logger = logger;
            _sqlConnectionFactory = sqlConnectionFactory;
            _schemaManagerDataStore = schemaManagerDataStore;
        }

        public async Task ApplySchemaAsync(int version, bool applyFullSchemaSnapshot, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Applying schema {version}", version);

                await _schemaManagerDataStore.DeleteSchemaVersionAsync(version, SchemaVersionStatus.failed.ToString(), cancellationToken).ConfigureAwait(false);

                if (!applyFullSchemaSnapshot)
                {
                    await InsertSchemaVersionAsync(version, cancellationToken);
                }

                await _schemaManagerDataStore.ExecuteScriptAsync(_scriptProvider.GetMigrationScript(version, applyFullSchemaSnapshot), cancellationToken);

                await CompleteSchemaVersionAsync(version, cancellationToken);

                _logger.LogInformation("Completed applying schema {version}", version);
            }
            catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
            {
                _logger.LogError(e, "Failed applying schema {version}", version);
                await FailSchemaVersionAsync(version, cancellationToken);
                throw;
            }
        }

        public async Task ApplyBaseSchemaAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Applying base schema");

            await _schemaManagerDataStore.ExecuteScriptAsync(_baseScriptProvider.GetScript(), cancellationToken);

            _logger.LogInformation("Completed applying base schema");
        }

        private async Task InsertSchemaVersionAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            await UpsertSchemaVersionAsync(schemaVersion, "started", cancellationToken);
        }

        private async Task CompleteSchemaVersionAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            await UpsertSchemaVersionAsync(schemaVersion, "completed", cancellationToken);
        }

        private async Task FailSchemaVersionAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            await UpsertSchemaVersionAsync(schemaVersion, "failed", cancellationToken);
        }

        private async Task UpsertSchemaVersionAsync(int schemaVersion, string status, CancellationToken cancellationToken)
        {
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", schemaVersion);
                upsertCommand.Parameters.AddWithValue("@status", status);

                await connection.TryOpenAsync(cancellationToken);
                await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
