﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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
        public const string Complete = "complete";

        public static void ExecuteQuery(string connectionString, string queryString, int version)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                try
                {
                    var server = new Server(new ServerConnection(connection));

                    server.ConnectionContext.ExecuteNonQuery(queryString);
                }
                catch (SqlException)
                {
                    ExecuteUpsert(connectionString, version, Failed);
                    throw;
                }
            }
        }

        public static void ExecuteDelete(string connectionString, int version, string status)
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

        public static void ExecuteUpsert(string connectionString, int version, string status)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection))
                {
                    upsertCommand.CommandType = CommandType.StoredProcedure;
                    upsertCommand.Parameters.AddWithValue("@version", version);
                    upsertCommand.Parameters.AddWithValue("@status", status);

                    upsertCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
