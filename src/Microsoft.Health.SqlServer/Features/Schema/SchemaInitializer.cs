// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;
using EnsureThat;
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
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly SchemaUpgradeRunner _schemaUpgradeRunner;
        private readonly SchemaInformation _schemaInformation;
        private readonly ILogger<SchemaInitializer> _logger;

        public SchemaInitializer(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration, SchemaUpgradeRunner schemaUpgradeRunner, SchemaInformation schemaInformation, ILogger<SchemaInitializer> logger)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(schemaUpgradeRunner, nameof(schemaUpgradeRunner));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _schemaUpgradeRunner = schemaUpgradeRunner;
            _schemaInformation = schemaInformation;
            _logger = logger;
        }

        public void Initialize(bool forceIncrementalSchemaUpgrade = false)
        {
            if (!CanInitialize())
            {
                return;
            }

            GetCurrentSchemaVersion();

            _logger.LogInformation("Schema version is {version}", _schemaInformation.Current?.ToString() ?? "NULL");

            if (_sqlServerDataStoreConfiguration.SchemaOptions.AutomaticUpdatesEnabled)
            {
                // If the stored procedure to get the current schema version doesn't exist
                if (_schemaInformation.Current == null)
                {
                    // Apply base schema
                    _schemaUpgradeRunner.ApplyBaseSchema();

                    // This is for tests purpose only
                    if (forceIncrementalSchemaUpgrade)
                    {
                        // Run version 1 and and apply .diff.sql files to upgrade the schema version.
                        _schemaUpgradeRunner.ApplySchema(version: 1, applyFullSchemaSnapshot: true);
                    }
                    else
                    {
                        // Apply the maximum supported version. This won't consider the .diff.sql files.
                        _schemaUpgradeRunner.ApplySchema(_schemaInformation.MaximumSupportedVersion, applyFullSchemaSnapshot: true);
                    }

                    GetCurrentSchemaVersion();
                }

                // If the current schema version needs to be upgraded
                if (_schemaInformation.Current < _schemaInformation.MaximumSupportedVersion)
                {
                    // Apply each .diff.sql file one by one.
                    int current = _schemaInformation.Current ?? 0;
                    for (int i = current + 1; i <= _schemaInformation.MaximumSupportedVersion; i++)
                    {
                        _schemaUpgradeRunner.ApplySchema(version: i, applyFullSchemaSnapshot: false);
                    }
                }

                GetCurrentSchemaVersion();
            }
        }

        private void GetCurrentSchemaVersion()
        {
            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                connection.Open();

                string tableName = "dbo.SchemaVersion";

                // Since now the status is made consistent as 'completed', we might have to check for 'complete' as well for the previous version's status
                using (var selectCommand = connection.CreateCommand())
                {
                    selectCommand.CommandText = string.Format(
                        "SELECT MAX(Version) FROM {0} " +
                        "WHERE Status = 'complete' OR Status = 'completed'", tableName);

                    try
                    {
                        _schemaInformation.Current = selectCommand.ExecuteScalar() as int?;
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

        public static SqlConnection GetConnectionIfDatabaseNotExists(string connectionString, string databaseName)
        {
            // Connect to master database to evaluate if the requested database exists
            var masterConnectionBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = string.Empty };
            var connection = new SqlConnection(masterConnectionBuilder.ToString());
            connection.Open();

            using (var checkDatabaseExistsCommand = connection.CreateCommand())
            {
                checkDatabaseExistsCommand.CommandText = "SELECT 1 FROM sys.databases where name = @databaseName";
                checkDatabaseExistsCommand.Parameters.AddWithValue("@databaseName", databaseName);
                if ((int?)checkDatabaseExistsCommand.ExecuteScalar() == 1)
                {
                    return null;
                }
            }

            return connection;
        }

        public static bool CreateDatabase(SqlConnection connection, string databaseName)
        {
            using (var canCreateDatabaseCommand = new SqlCommand("SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE DATABASE'", connection))
            {
                if ((int)canCreateDatabaseCommand.ExecuteScalar() > 0)
                {
                    using (var createDatabaseCommand = new SqlCommand($"CREATE DATABASE {databaseName}", connection))
                    {
                        createDatabaseCommand.ExecuteNonQuery();
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool CheckDatabasePermissions(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE TABLE'", connection))
                {
                    return (int)command.ExecuteScalar() > 0;
                }
            }
        }

        private bool CanInitialize()
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
                var connection = GetConnectionIfDatabaseNotExists(_sqlServerDataStoreConfiguration.ConnectionString, databaseName);

                if (connection != null)
                {
                    _logger.LogInformation("Database does not exist");

                    bool created = CreateDatabase(connection, databaseName);

                    if (created)
                    {
                        _logger.LogInformation("Created database");
                    }
                    else
                    {
                        _logger.LogWarning("Insufficient permissions to create the database");
                        return false;
                    }

                    connection.Close();
                }
            }

            // now switch to the target database

            bool canInitialize = CheckDatabasePermissions(_sqlServerDataStoreConfiguration.ConnectionString);

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
                Initialize();
            }
            else
            {
                _logger.LogCritical(
                    "There was no connection string supplied. Schema initialization can not be completed.");
            }
        }
    }
}
