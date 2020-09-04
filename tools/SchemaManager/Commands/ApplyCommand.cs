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
using Polly;
using SchemaManager.Exceptions;
using SchemaManager.Model;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public static class ApplyCommand
    {
        private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(20);
        private const int RetryAttempts = 3;

        public static async Task HandlerAsync(string connectionString, Uri server, MutuallyExclusiveType exclusiveType, bool force)
        {
            ISchemaClient schemaClient = new SchemaClient(server);

            if (force && !EnsureForce())
            {
                return;
            }

            try
            {
                // Base schema is required to run the schema migration tool.
                // This method also initializes the database if not initialized yet.
                BaseSchemaRunner.EnsureBaseSchemaExists(connectionString);

                // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
                // since the Schema job polls and upserts at the specified interval in the service.
                BaseSchemaRunner.EnsureInstanceSchemaRecordExists(connectionString);

                var availableVersions = await schemaClient.GetAvailability();

                if (availableVersions.Count <= 1)
                {
                    CommandUtils.PrintError(Resources.AvailableVersionsDefaultErrorMessage);
                    return;
                }

                availableVersions.Sort((x, y) => x.Id.CompareTo(y.Id));

                // Removes the current version since the first available version is always the current version which is already applied.
                // If the apply command is run immediately after the first run, then the service schema job might not poll the updated version
                // so there are retries to give it a fair amount of time to get the next available version.
                int attemptCount = 1;

                Policy.Handle<SchemaManagerException>()
                .WaitAndRetry(
                    retryCount: RetryAttempts,
                    sleepDurationProvider: (retryCount) => RetrySleepDuration,
                    onRetry: (exception, retryCount) =>
                    {
                        Console.WriteLine(string.Format(Resources.RetryCurrentSchemaVersion, attemptCount++, RetryAttempts));
                    })
                .Execute(() => CheckFirstAvailableVersionIsCurrentVersion(availableVersions.First().Id, connectionString));
                availableVersions.RemoveAt(0);

                var targetVersion = exclusiveType.Next == true ? availableVersions.First().Id :
                                                                 exclusiveType.Latest == true ? availableVersions.Last().Id :
                                                                                                        exclusiveType.Version;

                availableVersions = availableVersions.Where(availableVersion => availableVersion.Id <= targetVersion)
                    .ToList();

                // Checking the specified version is not out of range of available versions
                if (availableVersions.Count < 1 || targetVersion < availableVersions.First().Id || targetVersion > availableVersions.Last().Id)
                {
                    throw new SchemaManagerException(string.Format(Resources.SpecifiedVersionNotAvailable, targetVersion));
                }

                if (!force)
                {
                    attemptCount = 1;

                    await Policy.Handle<SchemaManagerException>()
                    .WaitAndRetryAsync(
                        retryCount: RetryAttempts,
                        sleepDurationProvider: (retryCount) => RetrySleepDuration,
                        onRetry: (exception, retryCount) =>
                        {
                            Console.WriteLine(string.Format(Resources.RetryVersionCompatibility, attemptCount++, RetryAttempts));
                        })
                    .ExecuteAsync(() => ValidateCompatibleVersion(schemaClient, availableVersions.First().Id, availableVersions.Last().Id));
                }
                else if (availableVersions.First().Id == 1)
                {
                    // Upgrade schema directly to the latest schema version
                    Console.WriteLine(string.Format(Resources.SchemaMigrationStartedMessage, availableVersions.Last().Id));

                    string script = await GetScript(schemaClient, 1, availableVersions.Last().ScriptUri);
                    UpgradeSchema(connectionString, availableVersions.Last().Id, script);
                    return;
                }

                foreach (AvailableVersion availableVersion in availableVersions)
                {
                    int executingVersion = availableVersion.Id;

                    Console.WriteLine(string.Format(Resources.SchemaMigrationStartedMessage, executingVersion));

                    if (!force)
                    {
                        attemptCount = 1;

                        await Policy.Handle<SchemaManagerException>()
                        .WaitAndRetryAsync(
                            retryCount: RetryAttempts,
                            sleepDurationProvider: (retryCount) => RetrySleepDuration,
                            onRetry: (exception, retryCount) =>
                            {
                                Console.WriteLine(string.Format(Resources.RetryCurrentVersions, attemptCount++, RetryAttempts));
                            })
                        .ExecuteAsync(() => ValidateInstancesVersion(schemaClient, executingVersion));
                    }

                    string script = await GetScript(schemaClient, executingVersion, availableVersion.ScriptUri, availableVersion.DiffUri);

                    UpgradeSchema(connectionString, executingVersion, script);
                }
            }
            catch (Exception ex) when (ex is SchemaManagerException || ex is InvalidOperationException)
            {
                CommandUtils.PrintError(ex.Message);
                return;
            }
            catch (HttpRequestException)
            {
                CommandUtils.PrintError(string.Format(Resources.RequestFailedMessage, server));
                return;
            }
            catch (Exception ex)
            {
                if (ex is SqlException || ex is ExecutionFailureException)
                {
                    CommandUtils.PrintError(string.Format(Resources.QueryExecutionErrorMessage, ex.Message));
                    return;
                }

                if (ex is SchemaManagerException || ex is InvalidOperationException)
                {
                    CommandUtils.PrintError(ex.Message);
                    return;
                }

                throw;
            }
        }

        private static void UpgradeSchema(string connectionString, int version, string script)
        {
            // check if the record for given version exists in failed status
            SchemaDataStore.DeleteSchemaVersion(connectionString, version, SchemaDataStore.Failed);

            SchemaDataStore.ExecuteScriptAndCompleteSchemaVersion(connectionString, script, version);

            Console.WriteLine(string.Format(Resources.SchemaMigrationSuccessMessage, version));
        }

        private static async Task<string> GetScript(ISchemaClient schemaClient, int version, string scriptUri, string diffUri = null)
        {
            if (version == 1)
            {
                return await schemaClient.GetScript(new Uri(scriptUri, UriKind.Relative));
            }

            return await schemaClient.GetDiffScript(new Uri(diffUri, UriKind.Relative));
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

        private static void CheckFirstAvailableVersionIsCurrentVersion(int firstAvailableVersion, string connectionString)
        {
            if (firstAvailableVersion != SchemaDataStore.GetCurrentSchemaVersion(connectionString))
            {
                throw new SchemaManagerException(Resources.CurrentSchemaVersionException);
            }
        }
    }
}
