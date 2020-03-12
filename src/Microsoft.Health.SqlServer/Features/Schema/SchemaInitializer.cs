﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlClient;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    /// <summary>
    /// EXPERIMENTAL - Initializes the sql schema and brings the schema up to the min supported version.
    /// The purpose of this it to enable easy scenarios during development and will likely be removed later.
    /// </summary>
    public class SchemaInitializer<TSchemaVersionEnum> : IStartable
        where TSchemaVersionEnum : Enum
    {
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly SchemaUpgradeRunner<TSchemaVersionEnum> _schemaUpgradeRunner;
        private readonly ISchemaInformation _schemaInformation;
        private readonly ILogger<SchemaInitializer<TSchemaVersionEnum>> _logger;

        public SchemaInitializer(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration, SchemaUpgradeRunner<TSchemaVersionEnum> schemaUpgradeRunner, ISchemaInformation schemaInformation, ILogger<SchemaInitializer<TSchemaVersionEnum>> logger)
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

        internal void Initialize(bool forceIncrementalSchemaUpgrade = false)
        {
            if (!CanInitialize())
            {
                return;
            }

            GetCurrentSchemaVersion();

            _logger.LogInformation("Schema version is {version}", _schemaInformation.Current?.ToString() ?? "NULL");

            // If the stored procedure to get the current schema version doesn't exist
            if (_schemaInformation.Current == null || _sqlServerDataStoreConfiguration.DeleteAllDataOnStartup)
            {
                if (forceIncrementalSchemaUpgrade)
                {
                    // Run version 1. We'll use this as a base schema and apply .diff.sql files to upgrade the schema version.
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

        private void GetCurrentSchemaVersion()
        {
            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                connection.Open();

                string procedureSchema = "dbo";
                string procedureName = "SelectCurrentSchemaVersion";

                bool procedureExists;
                using (var checkProcedureExistsCommand = connection.CreateCommand())
                {
                    checkProcedureExistsCommand.CommandText = @"
                        SELECT 1
                        FROM sys.procedures p
                        INNER JOIN sys.schemas s on p.schema_id = s.schema_id
                        WHERE s.name = @schemaName AND p.name = @procedureName";

                    checkProcedureExistsCommand.Parameters.AddWithValue("@schemaName", procedureSchema);
                    checkProcedureExistsCommand.Parameters.AddWithValue("@procedureName", procedureName);
                    procedureExists = checkProcedureExistsCommand.ExecuteScalar() != null;
                }

                if (!procedureExists)
                {
                    _logger.LogInformation("Procedure to select the schema version was not found. This must be a new database.");
                }
                else
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"{procedureSchema}.{procedureName}";
                        command.CommandType = CommandType.StoredProcedure;

                        _schemaInformation.Current = (int?)command.ExecuteScalar();
                    }
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

            if (_sqlServerDataStoreConfiguration.AllowDatabaseCreation)
            {
                // connect to master database to evaluate if the requested database exists
                var masterConnectionBuilder = new SqlConnectionStringBuilder(_sqlServerDataStoreConfiguration.ConnectionString) { InitialCatalog = string.Empty };
                using (var connection = new SqlConnection(masterConnectionBuilder.ToString()))
                {
                    connection.Open();

                    using (var checkDatabaseExistsCommand = connection.CreateCommand())
                    {
                        checkDatabaseExistsCommand.CommandText = "SELECT 1 FROM sys.databases where name = @databaseName";
                        checkDatabaseExistsCommand.Parameters.AddWithValue("@databaseName", databaseName);
                        bool exists = (int?)checkDatabaseExistsCommand.ExecuteScalar() == 1;

                        if (!exists)
                        {
                            _logger.LogInformation("Database does not exist");

                            using (var canCreateDatabaseCommand = new SqlCommand("SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE DATABASE'", connection))
                            {
                                if ((int)canCreateDatabaseCommand.ExecuteScalar() > 0)
                                {
                                    using (var createDatabaseCommand = new SqlCommand($"CREATE DATABASE {databaseName}", connection))
                                    {
                                        createDatabaseCommand.ExecuteNonQuery();
                                        _logger.LogInformation("Created database");
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Insufficient permissions to create the database");
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // now switch to the target database

            using (var connection = new SqlConnection(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                connection.Open();

                bool canInitialize;
                using (var command = new SqlCommand("SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE TABLE'", connection))
                {
                    canInitialize = (int)command.ExecuteScalar() > 0;
                }

                if (!canInitialize)
                {
                    _logger.LogWarning("Insufficient permissions to create tables in the database");
                }

                return canInitialize;
            }
        }

        public void Start()
        {
            if (!string.IsNullOrWhiteSpace(_sqlServerDataStoreConfiguration.ConnectionString))
            {
                Initialize();
            }
            else
            {
                _logger.LogCritical("There was no connection string supplied. Schema initialization can not be completed.");
            }
        }
    }
}
