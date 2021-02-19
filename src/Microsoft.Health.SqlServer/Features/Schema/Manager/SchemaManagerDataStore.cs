﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    public class SchemaManagerDataStore : ISchemaManagerDataStore
    {
        private ISqlConnectionFactory _sqlConnectionFactory;

        public SchemaManagerDataStore(ISqlConnectionFactory sqlConnectionFactory)
        {
            EnsureArg.IsNotNull(sqlConnectionFactory);

            _sqlConnectionFactory = sqlConnectionFactory;
        }

        /// <inheritdoc />
        public async Task ExecuteScriptAndCompleteSchemaVersionAsync(string script, int version, CancellationToken cancellationToken, bool isPaasScript)
        {
            EnsureArg.IsNotNull(script, nameof(script));
            EnsureArg.IsGte(version, 1);

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);
                ServerConnection serverConnection = new ServerConnection(connection);

                try
                {
                    var server = new Server(serverConnection);

                    serverConnection.BeginTransaction();

                    server.ConnectionContext.ExecuteNonQuery(script);

                    await UpsertVersionAsync(connection, version, SchemaVersionStatus.Completed.ToString(), isPaasScript, cancellationToken);

                    serverConnection.CommitTransaction();
                }
                catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
                {
                    serverConnection.RollBackTransaction();
                    await UpsertVersionAsync(connection, version, SchemaVersionStatus.Failed.ToString(), isPaasScript, cancellationToken);
                    throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task DeleteSchemaVersionAsync(int version, string status, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(status, nameof(status));
            EnsureArg.IsGte(version, 1);

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);

                var deleteQuery = "DELETE FROM dbo.SchemaVersion WHERE Version = @version AND Status = @status";
                using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@version", version);
                    deleteCommand.Parameters.AddWithValue("@status", status);

                    await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        /// <inheritdoc />
        public async Task DeletesPaasSchemaFailedRecordAsync(int version, CancellationToken cancellationToken)
        {
            EnsureArg.IsGte(version, 1);

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);

                var deleteQuery = "DELETE FROM dbo.PaasSchemaVersion WHERE Version = @version AND Status = @status";
                using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@version", version);
                    deleteCommand.Parameters.AddWithValue("@status", SchemaVersionStatus.Failed.ToString());

                    await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }
        }

        /// <inheritdoc />
        public async Task<int> GetCurrentSchemaVersionAsync(CancellationToken cancellationToken)
        {
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);

                try
                {
                    using (var selectCommand = new SqlCommand("dbo.SelectCurrentSchemaVersion", connection))
                    {
                        selectCommand.CommandType = CommandType.StoredProcedure;

                        object current = await selectCommand.ExecuteScalarAsync(cancellationToken);
                        return (current == null || Convert.IsDBNull(current)) ? 0 : (int)current;
                    }
                }
                catch (SqlException e) when (string.Equals(e.Message, Resources.CurrentSchemaVersionStoredProcedureNotFound, StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
            }
        }

        /// <inheritdoc />
        public async Task ExecuteScriptAsync(string script, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(script, nameof(script));

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);
                var server = new Server(new ServerConnection(connection));

                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }

        /// <inheritdoc />
        public async Task<bool> BaseSchemaExistsAsync(CancellationToken cancellationToken)
        {
            var procedureQuery = "SELECT COUNT(*) FROM sys.objects WHERE name = @name and type = @type";

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand(procedureQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", "SelectCurrentVersionsInformation");
                    command.Parameters.AddWithValue("@type", 'P');

                    return (int)await command.ExecuteScalarAsync(cancellationToken) != 0;
                }
            }
        }

        /// <inheritdoc />
        public async Task<bool> InstanceSchemaRecordExistsAsync(CancellationToken cancellationToken)
        {
            var procedureQuery = "SELECT COUNT(*) FROM dbo.InstanceSchema";

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand(procedureQuery, connection))
                {
                    return (int)await command.ExecuteScalarAsync() != 0;
                }
            }
        }

        /// <inheritdoc />
        public async Task CreatePaasSchemaTableIfNotExistsAsync(CancellationToken cancellationToken)
        {
            var createQuery = @"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'PaasSchemaVersion' and type = 'U')
                                    CREATE TABLE dbo.PaasSchemaVersion
                                    (
                                        Version int PRIMARY KEY,
                                        Status varchar(10)
                                    )";

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand(createQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsPaasSchemaRecordAsync(int version, string status, CancellationToken cancellationToken)
        {
            var selectQuery = "SELECT COUNT(*) FROM dbo.PaasSchemaVersion where version = @version and status = @status";

            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                await connection.OpenAsync(cancellationToken);

                using (var command = new SqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@version", version);
                    command.Parameters.AddWithValue("@status", status);
                    return (int)await command.ExecuteScalarAsync() != 0;
                }
            }
        }

        private async Task UpsertVersionAsync(SqlConnection connection, int version, string status, bool isPaasScript, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(connection, nameof(connection));
            EnsureArg.IsNotNull(status, nameof(status));
            EnsureArg.IsGte(version, 1);

            if (isPaasScript)
            {
                await UpsertPaasSchemaVersionAsync(connection, version, status, cancellationToken);
            }
            else
            {
                await UpsertSchemaVersionAsync(connection, version, status, cancellationToken);
            }
        }

        private static async Task UpsertSchemaVersionAsync(SqlConnection connection, int version, string status, CancellationToken cancellationToken)
        {
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", version);
                upsertCommand.Parameters.AddWithValue("@status", status);

                await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private static async Task UpsertPaasSchemaVersionAsync(SqlConnection connection, int version, string status, CancellationToken cancellationToken)
        {
            var insertQuery = "INSERT INTO dbo.PaasSchemaVersion VALUES(@version, @status)";

            using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@version", version);
                command.Parameters.AddWithValue("@status", status);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
