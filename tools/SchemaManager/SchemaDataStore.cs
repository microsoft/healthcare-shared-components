// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace SchemaManager
{
    public static class SchemaDataStore
    {
        public const string DeleteQuery = "DELETE FROM dbo.SchemaVersion WHERE Version = @version AND Status = @status";
        public const string Failed = "failed";
        public const string Completed = "completed";

        private static SqlConnectionWrapperFactory ConnectionWrapperFactory(string connectionString)
        {
            var sqlTransactionHandler = new SqlTransactionHandler();
            var sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
            var sqlCommandWrapperFactory = new SqlCommandWrapperFactory();
            sqlServerDataStoreConfiguration.ConnectionString = connectionString;

            return new SqlConnectionWrapperFactory(sqlServerDataStoreConfiguration, sqlTransactionHandler, sqlCommandWrapperFactory);
        }

        public static void ExecuteScript(string connectionString, string query, int version)
        {
            var sqlConnectionWrapperFactory = ConnectionWrapperFactory(connectionString);
            using (SqlConnectionWrapper sqlConnectionWrapper = sqlConnectionWrapperFactory.ObtainSqlConnectionWrapper(true))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnectionWrapper.SqlConnection);

                try
                {
                    var server = new Server(serverConnection);

                    serverConnection.BeginTransaction();
                    server.ConnectionContext.ExecuteNonQuery(query);
                    serverConnection.CommitTransaction();
                }
                catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
                {
                    serverConnection.RollBackTransaction();

                    // Set SchemaVersion status to failed
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        UpsertSchemaVersion(connection, version, Failed);
                    }

                    throw;
                }

                UpsertSchemaVersion(sqlConnectionWrapper.SqlConnection, version, Completed);
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

        public static void UpsertSchemaVersion(SqlConnection connection, int version, string status)
        {
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
