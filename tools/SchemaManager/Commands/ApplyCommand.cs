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
                var availableVersions = await schemaClient.GetAvailability();

                if (availableVersions.Count <= 1)
                {
                    CommandUtils.PrintError(Resources.AvailableVersionsDefaultErrorMessage);
                    return;
                }

                availableVersions.Sort((x, y) => x.Id.CompareTo(y.Id));

                // Removing the current version
                if (availableVersions.First().Id == SchemaDataStore.GetCurrentSchemaVersion(connectionString))
                {
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
                else if (availableVersions.First().Id == 1)
                {
                    string script = await GetScript(schemaClient, 1, availableVersions.Last().Script);
                    UpgradeSchema(connectionString, availableVersions.Last().Id, script);
                    return;
                }

                foreach (AvailableVersion availableVersion in availableVersions)
                {
                    int executingVersion = availableVersion.Id;
                    if (!force)
                    {
                        await ValidateInstancesVersion(schemaClient, executingVersion);
                    }

                    string script = await GetScript(schemaClient, executingVersion, availableVersion.Script);

                    UpgradeSchema(connectionString, executingVersion, script);

                    // to ensure server side polling is completed after each version migration
                    if (executingVersion != availableVersions.Last().Id)
                    {
                        Console.WriteLine(Resources.WaitMessage);
                        await Task.Delay(MaxWaitTime);
                    }
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

        private static void UpgradeSchema(string connectionString, int version, string script)
        {
            // check if the record for given version exists in failed status
            SchemaDataStore.DeleteSchemaVersion(connectionString, version, SchemaDataStore.Failed);

            SchemaDataStore.ExecuteScriptAndCompleteSchemaVersion(connectionString, script, version);

            Console.WriteLine(string.Format(Resources.SchemaMigrationSuccessMessage, version));
        }

        private static async Task<string> GetScript(ISchemaClient schemaClient, int version, Uri snapshotUri)
        {
            if (version == 1)
            {
                return await schemaClient.GetSnapshotScript(snapshotUri);
            }

            return await schemaClient.GetDiffScript(version);
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
