// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Common;
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
                _logger.LogError("Failed applying schema {version}", version);

                // check if the schema version is already completed by another instance in case multi-instance deployment
                // then don't override and just return
                if (await UpdateFailedStatusAsync(version, cancellationToken))
                {
                    throw;
                }
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
            await UpsertSchemaVersionAsync(schemaVersion, SchemaVersionStatus.started.ToString(), cancellationToken);
        }

        private async Task CompleteSchemaVersionAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            await UpsertSchemaVersionAsync(schemaVersion, SchemaVersionStatus.completed.ToString(), cancellationToken);
        }

        private async Task<bool> UpdateFailedStatusAsync(int schemaVersion, CancellationToken cancellationToken)
        {
            DbTransaction transaction;
            using (SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                await connection.TryOpenAsync(cancellationToken).ConfigureAwait(false);
                transaction = await connection.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (!await SchemaVersionCompletedAsync(schemaVersion, connection, cancellationToken))
                    {
                        await UpsertSchemaVersionAsync(schemaVersion, SchemaVersionStatus.failed.ToString(), cancellationToken, connection);
                        transaction.Commit();
                        return true;
                    }
                }
                catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return false;
        }

        private async Task UpsertSchemaVersionAsync(int schemaVersion, string status, CancellationToken cancellationToken, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
                {
                    await connection.TryOpenAsync(cancellationToken);
                    await UpsertSchemaVersionWithConnectionAsync(schemaVersion, connection, status, cancellationToken);
                }
            }
            else
            {
                await UpsertSchemaVersionWithConnectionAsync(schemaVersion, connection, status, cancellationToken);
            }
        }

        private static async Task UpsertSchemaVersionWithConnectionAsync(int schemaVersion, SqlConnection connection, string status, CancellationToken cancellationToken)
        {
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", schemaVersion);
                upsertCommand.Parameters.AddWithValue("@status", status);

                await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private static async Task<bool> SchemaVersionCompletedAsync(int schemaVersion, SqlConnection connection, CancellationToken cancellationToken)
        {
            EnsureArg.IsGte(schemaVersion, 1);

            bool exists = false;
            using (var selectCommand = new SqlCommand("SELECT * FROM dbo.SchemaVersion WHERE Version = @version AND Status = @status", connection))
            {
                selectCommand.Parameters.AddWithValue("@version", schemaVersion);
                selectCommand.Parameters.AddWithValue("@status", SchemaVersionStatus.completed.ToString());

                exists = (int)await selectCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != 0;
            }

            return exists;
        }
    }
}
