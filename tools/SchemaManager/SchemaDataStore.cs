// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SchemaManager
{
    public static class SchemaDataStore
    {
        public const string DeleteQuery = "DELETE FROM dbo.SchemaVersion WHERE Version = @version AND Status = @status";
        public const string Failed = "failed";
        public const string Completed = "completed";

        public static async Task ExecuteScriptAndCompleteSchemaVersionAsync(string connectionString, string script, int version)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                ServerConnection serverConnection = new ServerConnection(connection);

                try
                {
                    var server = new Server(serverConnection);

                    serverConnection.BeginTransaction();

                    server.ConnectionContext.ExecuteNonQuery(script);

                    await UpsertSchemaVersionAsync(connection, version, Completed);

                    serverConnection.CommitTransaction();
                }
                catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
                {
                    serverConnection.RollBackTransaction();
                    await UpsertSchemaVersionAsync(connection, version, Failed);
                    throw;
                }
            }
        }

        public static async Task DeleteSchemaVersionAsync(string connectionString, int version, string status)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var deleteCommand = new SqlCommand(DeleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@version", version);
                    deleteCommand.Parameters.AddWithValue("@status", status);

                    await deleteCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task<int> GetCurrentSchemaVersionAsync(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                try
                {
                    using (var selectCommand = new SqlCommand("dbo.SelectCurrentSchemaVersion", connection))
                    {
                        selectCommand.CommandType = CommandType.StoredProcedure;

                        object current = await selectCommand.ExecuteScalarAsync();
                        return (current == null || Convert.IsDBNull(current)) ? 0 : (int)current;
                    }
                }
                catch (SqlException e) when (string.Equals(e.Message, Resources.CurrentSchemaVersionStoredProcedureNotFound, StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
            }
        }

        private static async Task UpsertSchemaVersionAsync(SqlConnection connection, int version, string status)
        {
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", version);
                upsertCommand.Parameters.AddWithValue("@status", status);

                await upsertCommand.ExecuteNonQueryAsync();
            }
        }

        public static async Task ExecuteScript(string connectionString, string script)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var server = new Server(new ServerConnection(connection));

                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }

        public static async Task<bool> BaseSchemaExistsAsync(string connectionString)
        {
            var procedureQuery = "SELECT COUNT(*) FROM sys.objects WHERE name = @name and type = @type";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(procedureQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", "SelectCurrentVersionsInformation");
                    command.Parameters.AddWithValue("@type", 'P');

                    return (int)command.ExecuteScalar() == 0 ? false : true;
                }
            }
        }

        public static async Task<bool> InstanceSchemaRecordExistsAsync(string connectionString)
        {
            var procedureQuery = "SELECT COUNT(*) FROM dbo.InstanceSchema";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(procedureQuery, connection))
                {
                    return (int)command.ExecuteScalar() == 0 ? false : true;
                }
            }
        }
    }
}
