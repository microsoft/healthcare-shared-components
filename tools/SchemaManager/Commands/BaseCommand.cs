// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using SchemaManager.Exceptions;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public static class BaseCommand
    {
        public static void Handler(string connectionString, Service service)
        {
            IBaseScriptProvider baseScriptProvider = new BaseScriptProvider();

            try
            {
                // Execute common script
                if (!SchemaDataStore.PreMigrationSchemaExists(connectionString))
                {
                    var script = baseScriptProvider.GetCommonScript();

                    ExecuteScriptAndWriteMessage(connectionString, script, string.Empty);
                }
                else
                {
                    Console.WriteLine(string.Format(Resources.BaseSchemaAlreadyExists, string.Empty));
                }

                string serviceScript = null;

                switch (service)
                {
                    case Service.Fhir:
                        var serviceName = Enum.GetName(typeof(Service), Service.Fhir);
                        serviceScript = baseScriptProvider.GetServiceScript(serviceName);
                        ExecuteScriptAndWriteMessage(connectionString, serviceScript, serviceName);
                        break;
                    case Service.Dicom:
                        serviceName = Enum.GetName(typeof(Service), Service.Dicom);
                        serviceScript = baseScriptProvider.GetServiceScript(serviceName);
                        ExecuteScriptAndWriteMessage(connectionString, serviceScript, serviceName);
                        break;
                }
            }
            catch (SchemaManagerException ex)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
        }

        private static void ExecuteScriptAndWriteMessage(string connectionString, string script, string subMessage)
        {
            Console.WriteLine(string.Format(Resources.BaseSchemaExecuting, subMessage));

            SchemaDataStore.ExecuteScript(connectionString, script);

            Console.WriteLine(string.Format(Resources.BaseSchemaSuccess, subMessage));
        }
    }
}