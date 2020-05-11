// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using SchemaManager.Exceptions;
using SchemaManager.Model;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public static class ApplyCommand
    {
        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(1);

        public static async Task HandlerAsync(string connectionString, Uri server, MutuallyExclusiveType exclusiveType, bool force)
        {
            ISchemaClient schemaClient = new SchemaClient(server);

            if (force && !EnsureForce())
            {
                return;
            }

            try
            {
                List<AvailableVersion> availableVersions = await schemaClient.GetAvailability();

                if (availableVersions.Count <= 1)
                {
                    CommandUtils.PrintError(Resources.AvailableVersionsDefaultErrorMessage);
                    return;
                }
                else
                {
                    availableVersions.Sort((x, y) => x.Id.CompareTo(y.Id));

                    // Removing the current version
                    availableVersions.RemoveAt(0);
                }

                var targetVersion = exclusiveType.Next == true ? availableVersions.First().Id :
                                                                 exclusiveType.Latest == true ? availableVersions.Last().Id :
                                                                                                        exclusiveType.Version;

                availableVersions = availableVersions.Where(availableVersion => availableVersion.Id <= targetVersion)
                    .ToList();

                // to ensure server side polling is completed
                Console.WriteLine(Resources.WaitMessage);
                await Task.Delay(MaxWaitTime);

                if (!force)
                {
                    await ValidateCompatibleVersion(schemaClient, availableVersions.First().Id, availableVersions.Last().Id);
                }

                foreach (AvailableVersion availableVersion in availableVersions)
                {
                    string script = await schemaClient.GetScript(availableVersion.Script);

                    if (!force)
                    {
                        await ValidateInstancesVersion(schemaClient, availableVersion.Id);
                    }

                    // check if the record for given version exists in failed status
                    SchemaDataStore.DeleteSchemaVersion(connectionString, availableVersion.Id, SchemaDataStore.Failed);

                    SchemaDataStore.ExecuteScript(connectionString, script, availableVersion.Id);

                    Console.WriteLine(string.Format(Resources.SchemaMigrationSuccessMessage, availableVersion.Id));
                }
            }
            catch (SchemaManagerException ex)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
            catch (HttpRequestException)
            {
                CommandUtils.PrintError(string.Format(Resources.RequestFailedMessage, server));
                return;
            }
            catch (Exception ex) when (ex is SqlException || ex is ExecutionFailureException)
            {
                CommandUtils.PrintError(string.Format(Resources.QueryExecutionErrorMessage, ex.Message));
                return;
            }
        }

        private static async Task ValidateCompatibleVersion(ISchemaClient schemaClient, int minAvailableVersion, int maxAvailableVersion)
        {
            CompatibleVersion compatibleVersion = await schemaClient.GetCompatibility();

            // check if min and max available versions are not in compatibile range
            if (minAvailableVersion < compatibleVersion.Min || maxAvailableVersion > compatibleVersion.Max)
            {
                throw new SchemaManagerException(string.Format(Resources.VersionIncompatibilityMessage, maxAvailableVersion));
            }
        }

        private static async Task ValidateInstancesVersion(ISchemaClient schemaClient, int version)
        {
            List<CurrentVersion> currentVersions = await schemaClient.GetCurrentVersionInformation();

            // check if any instance is not running on the previous version
            if (currentVersions.Any(currentVersion => currentVersion.Id != (version - 1) && currentVersion.Servers.Count > 0))
            {
                throw new SchemaManagerException(string.Format(Resources.InvalidVersionMessage, version));
            }
        }

        private static bool EnsureForce()
        {
            Console.WriteLine(Resources.ForceWarning);
            return string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
