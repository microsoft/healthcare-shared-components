// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager
{
    public class BaseSchemaRunner
    {
        private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(20);
        private const int RetryAttempts = 3;
        private ISqlConnectionFactory _sqlConnectionFactory;
        private ISchemaManagerDataStore _schemaManagerDataStore;
        private SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

        public BaseSchemaRunner(
            ISqlConnectionFactory sqlConnectionFactory,
            ISchemaManagerDataStore schemaManagerDataStore,
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlConnectionFactory);
            EnsureArg.IsNotNull(schemaManagerDataStore);

            _sqlConnectionFactory = sqlConnectionFactory;
            _schemaManagerDataStore = schemaManagerDataStore;
            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
        }

        public async Task EnsureBaseSchemaExistsAsync(CancellationToken cancellationToken)
        {
            IBaseScriptProvider baseScriptProvider = new BaseScriptProvider();

            await InitializeAsync(cancellationToken);

            if (!await _schemaManagerDataStore.BaseSchemaExistsAsync(cancellationToken))
            {
                var script = baseScriptProvider.GetScript();

                Console.WriteLine(Resources.BaseSchemaExecuting);

                await _schemaManagerDataStore.ExecuteScriptAsync(script, cancellationToken);

                Console.WriteLine(Resources.BaseSchemaSuccess);
            }
            else
            {
                Console.WriteLine(Resources.BaseSchemaAlreadyExists);
            }
        }

        public async Task EnsureInstanceSchemaRecordExistsAsync(CancellationToken cancellationToken)
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
            .ExecuteAsync(token => InstanceSchemaRecordCreatedAsync(token), cancellationToken);
        }

        private async Task InstanceSchemaRecordCreatedAsync(CancellationToken cancellationToken)
        {
            if (!await _schemaManagerDataStore.InstanceSchemaRecordExistsAsync(cancellationToken))
            {
                throw new SchemaManagerException(Resources.InstanceSchemaRecordErrorMessage);
            }
        }

        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            var configuredConnectionBuilder = new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString);
            string databaseName = configuredConnectionBuilder.InitialCatalog;

            SchemaInitializer.ValidateDatabaseName(databaseName);

            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = string.Empty };

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
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(cancellationToken: cancellationToken))
            {
                canInitialize = await SchemaInitializer.CheckDatabasePermissionsAsync(connection, cancellationToken);
            }

            if (!canInitialize)
            {
                throw new SchemaManagerException(Resources.InsufficientTablesPermissionsMessage);
            }
        }
    }
}
