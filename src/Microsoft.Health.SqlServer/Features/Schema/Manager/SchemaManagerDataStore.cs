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
using Microsoft.Health.SqlServer.Extensions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    public class SchemaManagerDataStore : ISchemaManagerDataStore
    {
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public SchemaManagerDataStore(ISqlConnectionFactory sqlConnectionFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionFactory);

            _sqlConnectionFactory = sqlConnectionFactory;
        }

        /// <inheritdoc />
        public async Task ExecuteScriptAndCompleteSchemaVersionAsync(string script, int version, bool applyFullSchemaSnapshot, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(script, nameof(script));
            EnsureArg.IsGte(version, 1);

            using SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await connection.TryOpenAsync(cancellationToken).ConfigureAwait(false);
            var serverConnection = new ServerConnection(connection);

            try
            {
                // FullSchemaSnapshot script Inserts into SchemaVersion table with stated status
                if (!applyFullSchemaSnapshot)
                {
                    await UpsertSchemaVersionAsync(connection, version, SchemaVersionStatus.started.ToString(), cancellationToken).ConfigureAwait(false);
                }

                var server = new Server(serverConnection);
                server.ConnectionContext.ExecuteNonQuery(script);
                await UpsertSchemaVersionAsync(connection, version, SchemaVersionStatus.completed.ToString(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
            {
                await UpsertSchemaVersionAsync(connection, version, SchemaVersionStatus.failed.ToString(), cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteSchemaVersionAsync(int version, string status, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(status, nameof(status));
            EnsureArg.IsGte(version, 1);

            using SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await connection.TryOpenAsync(cancellationToken).ConfigureAwait(false);

            using var deleteCommand = new SqlCommand("DELETE FROM dbo.SchemaVersion WHERE Version = @version AND Status = @status", connection);
            deleteCommand.Parameters.AddWithValue("@version", version);
            deleteCommand.Parameters.AddWithValue("@status", status);

            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetCurrentSchemaVersionAsync(CancellationToken cancellationToken)
        {
            using SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await connection.TryOpenAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                using var selectCommand = new SqlCommand("dbo.SelectCurrentSchemaVersion", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                };

                object current = await selectCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return (current == null || Convert.IsDBNull(current)) ? 0 : (int)current;
            }
            catch (SqlException e) when (string.Equals(e.Message, Resources.CurrentSchemaVersionStoredProcedureNotFound, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
        }

        private static async Task UpsertSchemaVersionAsync(SqlConnection connection, int version, string status, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(connection, nameof(connection));
            EnsureArg.IsNotNull(status, nameof(status));
            EnsureArg.IsGte(version, 1);

            using var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection)
            {
                CommandType = CommandType.StoredProcedure,
            };
            upsertCommand.Parameters.AddWithValue("@version", version);
            upsertCommand.Parameters.AddWithValue("@status", status);

            await upsertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task ExecuteScriptAsync(string script, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(script, nameof(script));

            using SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await connection.TryOpenAsync(cancellationToken).ConfigureAwait(false);
            var server = new Server(new ServerConnection(connection));

            server.ConnectionContext.ExecuteNonQuery(script);
        }

        /// <inheritdoc />
        public async Task<bool> BaseSchemaExistsAsync(CancellationToken cancellationToken)
        {
            var procedureQuery = "SELECT COUNT(*) FROM sys.objects WHERE name = @name and type = @type";

            using SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await connection.TryOpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(procedureQuery, connection);
            command.Parameters.AddWithValue("@name", "SelectCurrentVersionsInformation");
            command.Parameters.AddWithValue("@type", 'P');

            return (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != 0;
        }

        /// <inheritdoc />
        public async Task<bool> InstanceSchemaRecordExistsAsync(CancellationToken cancellationToken)
        {
            var procedureQuery = "SELECT COUNT(*) FROM dbo.InstanceSchema";

            using SqlConnection connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await connection.TryOpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = new SqlCommand(procedureQuery, connection);
            return (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != 0;
        }
    }
}
