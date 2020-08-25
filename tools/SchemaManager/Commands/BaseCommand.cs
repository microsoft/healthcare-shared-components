// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Schema;
using SchemaManager.Exceptions;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public static class BaseCommand
    {
        public static void Handler(string connectionString)
        {
            IBaseScriptProvider baseScriptProvider = new BaseScriptProvider();

            try
            {
                Initialize(connectionString);

                // Execute common script
                var script = baseScriptProvider.GetScript();

                Console.WriteLine(Resources.BaseSchemaExecuting);

                SchemaDataStore.ExecuteScript(connectionString, script);

                Console.WriteLine(Resources.BaseSchemaSuccess);
            }
            catch (SchemaManagerException ex)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
            catch (InvalidOperationException ex)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
        }

        private static void Initialize(string connectionString)
        {
            if (!CanInitialize(connectionString))
            {
                return;
            }
        }

        private static bool CanInitialize(string connectionString)
        {
            var configuredConnectionBuilder = new SqlConnectionStringBuilder(connectionString);
            string databaseName = configuredConnectionBuilder.InitialCatalog;

            SchemaInitializer.ValidateDatabaseName(databaseName);

            var connection = SchemaInitializer.GetConnectionIfDatabaseNotExists(connectionString, databaseName);

            // database creation is allowed
            if (connection != null)
            {
                Console.WriteLine("Database does not exists.");

                bool created = SchemaInitializer.CreateDatabase(connection, databaseName);

                if (created)
                {
                    Console.WriteLine("Created database");
                }
                else
                {
                    Console.WriteLine("Insufficient permissions to create the database");
                    return false;
                }

                connection.Close();
            }

            // now switch to the target database

            bool canInitialize = SchemaInitializer.CheckDatabasePermissions(connectionString);

            if (!canInitialize)
            {
                Console.WriteLine("Insufficient permissions to create tables in the database");
            }

            return canInitialize;
        }
    }
}