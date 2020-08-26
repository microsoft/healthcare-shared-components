// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SchemaManager
{
    public static class SchemaDataStore
    {
        public const string DeleteQuery = "DELETE FROM dbo.SchemaVersion WHERE Version = @version AND Status = @status";
        public const string Failed = "failed";
        public const string Completed = "completed";

        public static void ExecuteScriptAndCompleteSchemaVersion(string connectionString, string script, int version)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                ServerConnection serverConnection = new ServerConnection(connection);

                try
                {
                    var server = new Server(serverConnection);

                    serverConnection.BeginTransaction();
                    server.ConnectionContext.ExecuteNonQuery(script);

                    UpsertSchemaVersion(connection, version, Completed);

                    serverConnection.CommitTransaction();
                }
                catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
                {
                    serverConnection.RollBackTransaction();
                    UpsertSchemaVersion(connection, version, Failed);
                    throw;
                }
            }
        }

        public static void DeleteSchemaVersion(string connectionString, int version, string status)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var deleteCommand = new SqlCommand(DeleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@version", version);
                    deleteCommand.Parameters.AddWithValue("@status", status);

                    deleteCommand.ExecuteNonQuery();
                }
            }
        }

        public static int GetCurrentSchemaVersion(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    using (var selectCommand = new SqlCommand("dbo.SelectCurrentSchemaVersion", connection))
                    {
                        selectCommand.CommandType = CommandType.StoredProcedure;

                        object current = selectCommand.ExecuteScalar();
                        return (current == null || Convert.IsDBNull(current)) ? 0 : (int)current;
                    }
                }
                catch (SqlException e) when (string.Equals(e.Message, Resources.CurrentSchemaVersionStoredProcedureNotFound, StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }
            }
        }

        private static void UpsertSchemaVersion(SqlConnection connection, int version, string status)
        {
            using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
            {
                upsertCommand.CommandType = CommandType.StoredProcedure;
                upsertCommand.Parameters.AddWithValue("@version", version);
                upsertCommand.Parameters.AddWithValue("@status", status);

                upsertCommand.ExecuteNonQuery();
            }
        }

        public static void ExecuteScript(string connectionString, string script)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var server = new Server(new ServerConnection(connection));

                server.ConnectionContext.ExecuteNonQuery(script);
            }
        }

        public static bool BaseSchemaExists(string connectionString)
        {
            var procedureQuery = "SELECT COUNT(*) FROM sys.objects WHERE name = @name and type = @type";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(procedureQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", "SelectCurrentVersionsInformation");
                    command.Parameters.AddWithValue("@type", 'P');

                    return (int)command.ExecuteScalar() == 0 ? false : true;
                }
            }
        }

        public static bool InstanceSchemaRecordExists(string connectionString)
        {
            var procedureQuery = "SELECT COUNT(*) FROM dbo.InstanceSchema";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(procedureQuery, connection))
                {
                    return (int)command.ExecuteScalar() == 0 ? false : true;
                }
            }
        }
    }
}
