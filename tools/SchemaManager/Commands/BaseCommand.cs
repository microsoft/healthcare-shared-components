// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace SchemaManager.Commands
{
    public static class BaseCommand
    {
        public static void Handler(string connectionString)
        {
            IBaseScriptProvider baseScriptProvider = new BaseScriptProvider();

            if (!SchemaDataStore.PreMigrationSchemaExists(connectionString))
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
    }
}