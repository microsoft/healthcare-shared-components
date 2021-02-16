﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Microsoft.SqlServer.Management.Common;
using Polly;
using SchemaManager.Model;
using SchemaManager.Utils;

namespace SchemaManager.Commands
{
    public class ApplyCommand : Command
    {
        private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(20);
        private const int RetryAttempts = 3;
        private static List<AvailableVersion> availableVersions;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly BaseSchemaRunner _baseSchemaRunner;
        private readonly ISchemaManagerDataStore _schemaManagerDataStore;
        private readonly ISchemaClient _schemaClient;

        public ApplyCommand(
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
            BaseSchemaRunner baseSchemaRunner,
            ISchemaManagerDataStore schemaManagerDataStore,
            ISchemaClient schemaClient)
            : base(CommandNames.Apply, Resources.ApplyCommandDescription)
        {
            AddOption(CommandOptions.ConnectionStringOption());
            AddOption(CommandOptions.ServerOption());
            AddOption(CommandOptions.VersionOption());
            AddOption(CommandOptions.NextOption());
            AddOption(CommandOptions.LatestOption());
            AddOption(CommandOptions.ForceOption());

            Handler = CommandHandler.Create(
                (string connectionString, Uri server, MutuallyExclusiveType type, bool force, CancellationToken token)
                => HandlerAsync(connectionString, server, type, force, token));

            Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, CommandOptions.ConnectionStringOption(), Resources.ConnectionStringRequiredValidation));
            Argument.AddValidator(symbol => Validators.RequiredOptionValidator.Validate(symbol, CommandOptions.ServerOption(), Resources.ServerRequiredValidation));
            Argument.AddValidator(symbol => Validators.MutuallyExclusiveOptionValidator.Validate(symbol, new List<Option> { CommandOptions.VersionOption(), CommandOptions.NextOption(), CommandOptions.LatestOption() }, Resources.MutuallyExclusiveValidation));

            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration);
            EnsureArg.IsNotNull(baseSchemaRunner);
            EnsureArg.IsNotNull(schemaManagerDataStore);
            EnsureArg.IsNotNull(schemaClient);

            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _baseSchemaRunner = baseSchemaRunner;
            _schemaManagerDataStore = schemaManagerDataStore;
            _schemaClient = schemaClient;
        }

        private async Task HandlerAsync(string connectionString, Uri server, MutuallyExclusiveType exclusiveType, bool force, CancellationToken cancellationToken = default)
        {
            _schemaClient.SetUri(server);

            _sqlServerDataStoreConfiguration.ConnectionString = connectionString;

            if (force && !EnsureForce())
            {
                return;
            }

            try
            {
                _sqlServerDataStoreConfiguration.ConnectionString = connectionString;

                // Base schema is required to run the schema migration tool.
                // This method also initializes the database if not initialized yet.
                await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(cancellationToken);

                // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
                // since the Schema job polls and upserts at the specified interval in the service.
                await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(cancellationToken);

                availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken);

                // If the user hits apply command multiple times in a row, then the service schema job might not poll the updated available versions
                // so there are retries to give it a fair amount of time.
                int attemptCount = 1;

                await Policy.Handle<SchemaManagerException>()
                .WaitAndRetryAsync(
                    retryCount: RetryAttempts,
                    sleepDurationProvider: (retryCount) => RetrySleepDuration,
                    onRetry: (exception, retryCount) =>
                    {
                        Console.WriteLine(string.Format(Resources.RetryCurrentSchemaVersion, attemptCount++, RetryAttempts));
                    })
                .ExecuteAsync(token => FetchUpdatedAvailableVersionsAsync(token), cancellationToken);

                if (availableVersions.Count == 1)
                {
                    CommandUtils.PrintError(Resources.AvailableVersionsDefaultErrorMessage);
                    return;
                }

                // Removes the current version since the first available version is always the current version which is already applied.
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
                    await ValidateVersionCompatibility(availableVersions.Last().Id, cancellationToken);
                }

                if (availableVersions.First().Id == 1)
                {
                    // Upgrade schema directly to the latest schema version
                    Console.WriteLine(string.Format(Resources.SchemaMigrationStartedMessage, availableVersions.Last().Id));

                    string script = await GetScriptAsync(1, availableVersions.Last().ScriptUri, cancellationToken);
                    await UpgradeSchemaAsync(availableVersions.Last().Id, script, cancellationToken);
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
                        .ExecuteAsync(token => ValidateInstancesVersionAsync(executingVersion, token), cancellationToken);
                    }

                    string script = await GetScriptAsync(executingVersion, availableVersion.ScriptUri, cancellationToken, availableVersion.DiffUri);

                    await UpgradeSchemaAsync(executingVersion, script, cancellationToken);
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

                throw;
            }
        }

        private async Task UpgradeSchemaAsync(int version, string script, CancellationToken cancellationToken)
        {
            // check if the record for given version exists in failed status
            await _schemaManagerDataStore.DeleteSchemaVersionAsync(version, SchemaVersionStatus.Failed.ToString(), cancellationToken);

            await _schemaManagerDataStore.ExecuteScriptAndCompleteSchemaVersionAsync(script, version, cancellationToken);

            Console.WriteLine(string.Format(Resources.SchemaMigrationSuccessMessage, version));
        }

        private async Task<string> GetScriptAsync(int version, string scriptUri, CancellationToken cancellationToken, string diffUri = null)
        {
            if (version == 1)
            {
                return await _schemaClient.GetScriptAsync(new Uri(scriptUri, UriKind.Relative), cancellationToken);
            }

            return await _schemaClient.GetDiffScriptAsync(new Uri(diffUri, UriKind.Relative), cancellationToken);
        }

        private async Task ValidateVersionCompatibility(int maxAvailableVersion, CancellationToken cancellationToken)
        {
            CompatibleVersion compatibleVersion = await _schemaClient.GetCompatibilityAsync(cancellationToken);

            if (maxAvailableVersion > compatibleVersion.Max)
            {
                throw new SchemaManagerException(string.Format(Resources.VersionIncompatibilityMessage, maxAvailableVersion));
            }
        }

        private async Task ValidateInstancesVersionAsync(int version, CancellationToken cancellationToken)
        {
            List<CurrentVersion> currentVersions = await _schemaClient.GetCurrentVersionInformationAsync(cancellationToken);

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

        private async Task FetchUpdatedAvailableVersionsAsync(CancellationToken cancellationToken)
        {
            availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken);

            availableVersions.Sort((x, y) => x.Id.CompareTo(y.Id));

            if (availableVersions.First().Id != await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken))
            {
                throw new SchemaManagerException(Resources.AvailableVersionsErrorMessage);
            }
        }
    }
}
