// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Schema;
using Polly;
using SchemaManager.Exceptions;

namespace SchemaManager.Utils
{
    public static class BaseSchemaRunner
    {
        private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(20);
        private const int RetryAttempts = 3;

        public static async Task EnsureBaseSchemaExistsAsync(string connectionString, CancellationToken cancellationToken)
        {
            IBaseScriptProvider baseScriptProvider = new BaseScriptProvider();

            await InitializeAsync(connectionString, cancellationToken);

            if (!await SchemaDataStore.BaseSchemaExistsAsync(connectionString))
            {
                var script = baseScriptProvider.GetScript();

                Console.WriteLine(Resources.BaseSchemaExecuting);

                await SchemaDataStore.ExecuteScript(connectionString, script);

                Console.WriteLine(Resources.BaseSchemaSuccess);
            }
            else
            {
                Console.WriteLine(Resources.BaseSchemaAlreadyExists);
            }
        }

        public static async Task EnsureInstanceSchemaRecordExistsAsync(string connectionString)
        {
            // Ensure that the current version record is inserted into InstanceSchema table
            int attempts = 1;

            await Policy.Handle<SchemaManagerException>()
            .WaitAndRetryAsync(
                retryCount: RetryAttempts,
                sleepDurationProvider: (retryCount) => RetrySleepDuration,
                onRetry: (exception, retryCount) =>
                {
                    Console.WriteLine(string.Format(Resources.RetryInstanceSchemaRecord, attempts++, RetryAttempts));
                })
            .ExecuteAsync(() => InstanceSchemaRecordCreatedAsync(connectionString));
        }

        private static async Task InstanceSchemaRecordCreatedAsync(string connectionString)
        {
            if (!await SchemaDataStore.InstanceSchemaRecordExistsAsync(connectionString))
            {
                throw new SchemaManagerException(Resources.InstanceSchemaRecordErrorMessage);
            }
        }

        private static async Task InitializeAsync(string connectionString, CancellationToken cancellationToken)
        {
            var configuredConnectionBuilder = new SqlConnectionStringBuilder(connectionString);
            string databaseName = configuredConnectionBuilder.InitialCatalog;

            SchemaInitializer.ValidateDatabaseName(databaseName);

            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = string.Empty };

            using (var connection = new SqlConnection(connectionBuilder.ToString()))
            {
                bool doesDatabaseExist = await SchemaInitializer.DoesDatabaseExistAsync(connection, databaseName, cancellationToken);

                // database creation is allowed
                if (!doesDatabaseExist)
                {
                    Console.WriteLine("The database does not exists.");

                    bool created = await SchemaInitializer.CreateDatabaseAsync(connection, databaseName, cancellationToken);

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
                canInitialize = await SchemaInitializer.CheckDatabasePermissionsAsync(sqlConnection, cancellationToken);
            }

            if (!canInitialize)
            {
                throw new SchemaManagerException(Resources.InsufficientTablesPermissionsMessage);
            }
        }
    }
}
