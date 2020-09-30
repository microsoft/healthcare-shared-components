// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Schema;
using Polly;
using SchemaManager.Exceptions;

namespace SchemaManager.Utils
{
    public static class BaseSchemaRunner
    {
        private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(20);
        private const int RetryAttempts = 3;

        public static void EnsureBaseSchemaExists(string connectionString)
        {
            IBaseScriptProvider baseScriptProvider = new BaseScriptProvider();

            Initialize(connectionString);

            if (!SchemaDataStore.BaseSchemaExists(connectionString))
            {
                var script = baseScriptProvider.GetScript();

                Console.WriteLine(Resources.BaseSchemaExecuting);

                SchemaDataStore.ExecuteScript(connectionString, script);

                Console.WriteLine(Resources.BaseSchemaSuccess);
            }
            else
            {
                Console.WriteLine(Resources.BaseSchemaAlreadyExists);
            }
        }

        public static void EnsureInstanceSchemaRecordExists(string connectionString)
        {
            // Ensure that the current version record is inserted into InstanceSchema table
            int attempts = 1;

            Policy.Handle<SchemaManagerException>()
            .WaitAndRetry(
                retryCount: RetryAttempts,
                sleepDurationProvider: (retryCount) => RetrySleepDuration,
                onRetry: (exception, retryCount) =>
                {
                    Console.WriteLine(string.Format(Resources.RetryInstanceSchemaRecord, attempts++, RetryAttempts));
                })
            .Execute(() => InstanceSchemaRecordCreated(connectionString));
        }

        private static void InstanceSchemaRecordCreated(string connectionString)
        {
            if (!SchemaDataStore.InstanceSchemaRecordExists(connectionString))
            {
                throw new SchemaManagerException(Resources.InstanceSchemaRecordErrorMessage);
            }
        }

        private static void Initialize(string connectionString)
        {
            var configuredConnectionBuilder = new SqlConnectionStringBuilder(connectionString);
            string databaseName = configuredConnectionBuilder.InitialCatalog;

            SchemaInitializer.ValidateDatabaseName(databaseName);

            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = string.Empty };

            using (var connection = new SqlConnection(connectionBuilder.ToString()))
            {
                bool doesDatabaseExist = SchemaInitializer.DoesDatabaseExist(connection, databaseName);

                // database creation is allowed
                if (!doesDatabaseExist)
                {
                    Console.WriteLine("The database does not exists.");

                    bool created = SchemaInitializer.CreateDatabase(connection, databaseName);

                    if (created)
                    {
                        Console.WriteLine("The database is created.");
                    }
                    else
                    {
                        throw new SchemaManagerException(Resources.InsufficientDatabasePermissionsMessage);
                    }

                    connection.Close();
                }
            }

            bool canInitialize = false;

            // now switch to the target database
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                canInitialize = SchemaInitializer.CheckDatabasePermissions(sqlConnection);
            }

            if (!canInitialize)
            {
                throw new SchemaManagerException(Resources.InsufficientTablesPermissionsMessage);
            }
        }
    }
}
