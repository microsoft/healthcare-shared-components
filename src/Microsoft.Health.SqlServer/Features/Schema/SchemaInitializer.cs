// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    /// <summary>
    /// EXPERIMENTAL - Initializes the sql schema and brings the schema up to the min supported version.
    /// The purpose of this it to enable easy scenarios during development and will likely be removed later.
    /// </summary>
    public class SchemaInitializer : IStartable
    {
        private const string MasterDatabase = "master";
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly SchemaUpgradeRunner _schemaUpgradeRunner;
        private readonly SchemaInformation _schemaInformation;
        private readonly ILogger<SchemaInitializer> _logger;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public SchemaInitializer(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration, SchemaUpgradeRunner schemaUpgradeRunner, SchemaInformation schemaInformation, ISqlConnectionFactory sqlConnectionFactory, ILogger<SchemaInitializer> logger)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(schemaUpgradeRunner, nameof(schemaUpgradeRunner));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(sqlConnectionFactory, nameof(sqlConnectionFactory));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _schemaUpgradeRunner = schemaUpgradeRunner;
            _schemaInformation = schemaInformation;
            _sqlConnectionFactory = sqlConnectionFactory;
            _logger = logger;
        }

        public async Task InitializeAsync(bool forceIncrementalSchemaUpgrade = false)
        {
            if (!await CanInitializeAsync())
            {
                return;
            }

            await GetCurrentSchemaVersionAsync();

            _logger.LogInformation("Schema version is {version}", _schemaInformation.Current?.ToString() ?? "NULL");

            if (_sqlServerDataStoreConfiguration.SchemaOptions.AutomaticUpdatesEnabled)
            {
                // If the stored procedure to get the current schema version doesn't exist
                if (_schemaInformation.Current == null)
                {
                    // Apply base schema
                    await _schemaUpgradeRunner.ApplyBaseSchema();

                    // This is for tests purpose only
                    if (forceIncrementalSchemaUpgrade)
                    {
                        // Run version 1 and and apply .diff.sql files to upgrade the schema version.
                        await _schemaUpgradeRunner.ApplySchema(_schemaInformation.MinimumSupportedVersion, applyFullSchemaSnapshot: true);
                    }
                    else
                    {
                        // Apply the maximum supported version. This won't consider the .diff.sql files.
                        await _schemaUpgradeRunner.ApplySchema(_schemaInformation.MaximumSupportedVersion, applyFullSchemaSnapshot: true);
                    }

                    await GetCurrentSchemaVersionAsync();
                }

                // If the current schema version needs to be upgraded
                if (_schemaInformation.Current < _schemaInformation.MaximumSupportedVersion)
                {
                    // Apply each .diff.sql file one by one.
                    int current = _schemaInformation.Current ?? 0;
                    for (int i = current + 1; i <= _schemaInformation.MaximumSupportedVersion; i++)
                    {
                        await _schemaUpgradeRunner.ApplySchema(version: i, applyFullSchemaSnapshot: false);
                    }
                }

                await GetCurrentSchemaVersionAsync();
            }
        }

        private async Task GetCurrentSchemaVersionAsync()
        {
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync())
            {
                await connection.OpenAsync();

                string tableName = "dbo.SchemaVersion";

                // Since now the status is made consistent as 'completed', we might have to check for 'complete' as well for the previous version's status
                using (var selectCommand = connection.CreateCommand())
                {
                    selectCommand.CommandText = string.Format(
                        "SELECT MAX(Version) FROM {0} " +
                        "WHERE Status = 'complete' OR Status = 'completed'", tableName);

                    try
                    {
                        _schemaInformation.Current = await selectCommand.ExecuteScalarAsync() as int?;
                    }
                    catch (SqlException e) when (e.Message is "Invalid object name 'dbo.SchemaVersion'.")
                    {
                        _logger.LogInformation($"The table {tableName} does not exists. It must be new database");
                    }
                }
            }
        }

        public static void ValidateDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new InvalidOperationException("The initial catalog must be specified in the connection string");
            }

            if (databaseName.Equals("master", StringComparison.OrdinalIgnoreCase) ||
                databaseName.Equals("model", StringComparison.OrdinalIgnoreCase) ||
                databaseName.Equals("msdb", StringComparison.OrdinalIgnoreCase) ||
                databaseName.Equals("tempdb", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The initial catalog in the connection string cannot be a system database");
            }
        }

        /// <summary>
        /// Check if the database exists.
        /// </summary>
        /// <param name="sqlConnection">Sql Connection with permissions to query the system tables in order to check if the provided database exists.</param>
        /// <param name="databaseName">Database name.</param>
        /// <returns>True if database exists, else returns false.</returns>
        public static async Task<bool> DoesDatabaseExistAsync(SqlConnection sqlConnection, string databaseName)
        {
            EnsureArg.IsNotNull(sqlConnection, nameof(sqlConnection));

            if (sqlConnection.State != ConnectionState.Open)
            {
                await sqlConnection.OpenAsync();
            }

            using (var checkDatabaseExistsCommand = sqlConnection.CreateCommand())
            {
                checkDatabaseExistsCommand.CommandText = "SELECT 1 FROM sys.databases where name = @databaseName";
                checkDatabaseExistsCommand.Parameters.AddWithValue("@databaseName", databaseName);
                if ((int?)await checkDatabaseExistsCommand.ExecuteScalarAsync() == 1)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> CreateDatabaseAsync(SqlConnection connection, string databaseName)
        {
            using (var canCreateDatabaseCommand = new SqlCommand("SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE DATABASE'", connection))
            {
                if ((int)await canCreateDatabaseCommand.ExecuteScalarAsync() > 0)
                {
                    using (var createDatabaseCommand = new SqlCommand($"CREATE DATABASE {databaseName}", connection))
                    {
                        await createDatabaseCommand.ExecuteNonQueryAsync();
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static async Task<bool> CheckDatabasePermissionsAsync(SqlConnection connection)
        {
            EnsureArg.IsNotNull(connection, nameof(connection));

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using (var command = new SqlCommand("SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE TABLE'", connection))
            {
                return (int)await command.ExecuteScalarAsync() > 0;
            }
        }

        private async Task<bool> CanInitializeAsync()
        {
            if (!_sqlServerDataStoreConfiguration.Initialize)
            {
                return false;
            }

            var configuredConnectionBuilder = new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString);
            string databaseName = configuredConnectionBuilder.InitialCatalog;

            ValidateDatabaseName(databaseName);

            if (_sqlServerDataStoreConfiguration.AllowDatabaseCreation)
            {
                using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync(MasterDatabase))
                {
                    bool doesDatabaseExist = await DoesDatabaseExistAsync(connection, databaseName);

                    if (!doesDatabaseExist)
                    {
                        _logger.LogInformation("Database does not exist");

                        bool created = await CreateDatabaseAsync(connection, databaseName);

                        if (created)
                        {
                            _logger.LogInformation("Created database");
                        }
                        else
                        {
                            _logger.LogWarning("Insufficient permissions to create the database");
                            return false;
                        }
                    }
                }
            }

            bool canInitialize = false;

            // now switch to the target database
            using (var connection = await _sqlConnectionFactory.GetSqlConnectionAsync())
            {
                canInitialize = await CheckDatabasePermissionsAsync(connection);
            }

            if (!canInitialize)
            {
                _logger.LogWarning("Insufficient permissions to create tables in the database");
            }

            return canInitialize;
        }

        public void Start()
        {
            if (!string.IsNullOrWhiteSpace(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                InitializeAsync().Wait();
            }
            else
            {
                _logger.LogCritical(
                    "There was no connection string supplied. Schema initialization can not be completed.");
            }
        }
    }
}
