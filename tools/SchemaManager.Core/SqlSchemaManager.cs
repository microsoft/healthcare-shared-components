// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Microsoft.SqlServer.Management.Common;
using Polly;
using SchemaManager.Core.Model;

namespace SchemaManager.Core
{
    public class SqlSchemaManager : ISchemaManager
    {
        private SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private BaseSchemaRunner _baseSchemaRunner;
        private ISchemaManagerDataStore _schemaManagerDataStore;
        private ISchemaClient _schemaClient;
        private ILogger<SqlSchemaManager> _logger;

        private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(20);
        private const int RetryAttempts = 3;

        public SqlSchemaManager(
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
            BaseSchemaRunner baseSchemaRunner,
            ISchemaManagerDataStore schemaManagerDataStore,
            ISchemaClient schemaClient,
            ILogger<SqlSchemaManager> logger)
        {
            _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            _baseSchemaRunner = EnsureArg.IsNotNull(baseSchemaRunner, nameof(baseSchemaRunner));
            _schemaManagerDataStore = EnsureArg.IsNotNull(schemaManagerDataStore, nameof(schemaManagerDataStore));
            _schemaClient = EnsureArg.IsNotNull(schemaClient, nameof(schemaClient));
            _logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        /// <inheritdoc />
        public async Task ApplySchema(string connectionString, Uri server, MutuallyExclusiveType exclusiveType, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNullOrEmpty(connectionString, nameof(connectionString));
            EnsureArg.IsNotNull(server, nameof(server));
            EnsureArg.IsNotNull(exclusiveType, nameof(exclusiveType));

            _schemaClient.SetUri(server);

            try
            {
                _sqlServerDataStoreConfiguration.ConnectionString = connectionString;

                // Base schema is required to run the schema migration tool.
                // This method also initializes the database if not initialized yet.
                await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(cancellationToken);

                // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
                // since the Schema job polls and upserts at the specified interval in the service.
                await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(cancellationToken);

                await ExecutePaasSchemaIfExistsAsync(cancellationToken);

                var availableVersions = (await GetAvailableSchema(server, cancellationToken)).ToList();

                // If the user hits apply command multiple times in a row, then the service schema job might not poll the updated available versions
                // so there are retries to give it a fair amount of time.
                int attemptCount = 1;

                availableVersions = await Policy.Handle<SchemaManagerException>()
                .WaitAndRetryAsync(
                    retryCount: RetryAttempts,
                    sleepDurationProvider: (retryCount) => RetrySleepDuration,
                    onRetry: (exception, retryCount) =>
                    {
                        _logger.LogError(exception, string.Format(Resources.RetryCurrentSchemaVersion, attemptCount++, RetryAttempts));
                    })
                .ExecuteAsync(token => FetchUpdatedAvailableVersionsAsync(token), cancellationToken);

                if (availableVersions.Count() == 1)
                {
                    _logger.LogError(Resources.AvailableVersionsDefaultErrorMessage);
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

                await ValidateVersionCompatibility(availableVersions.Last().Id, cancellationToken);

                if (availableVersions.First().Id == 1)
                {
                    // Upgrade schema directly to the latest schema version
                    _logger.LogInformation(string.Format(Resources.SchemaMigrationStartedMessage, availableVersions.Last().Id));

                    string script = await GetScriptAsync(1, availableVersions.Last().ScriptUri, cancellationToken);
                    await UpgradeSchemaAsync(availableVersions.Last().Id, script, cancellationToken);
                    return;
                }

                foreach (AvailableVersion availableVersion in availableVersions)
                {
                    int executingVersion = availableVersion.Id;

                    _logger.LogInformation(string.Format(Resources.SchemaMigrationStartedMessage, executingVersion));

                    attemptCount = 1;

                    await Policy.Handle<SchemaManagerException>()
                        .WaitAndRetryAsync(
                            retryCount: RetryAttempts,
                            sleepDurationProvider: (retryCount) => RetrySleepDuration,
                            onRetry: (exception, retryCount) =>
                            {
                                _logger.LogError(exception, string.Format(Resources.RetryCurrentVersions, attemptCount++, RetryAttempts));
                            })
                        .ExecuteAsync(token => ValidateInstancesVersionAsync(executingVersion, token), cancellationToken);

                    string script = await GetScriptAsync(executingVersion, availableVersion.ScriptUri, cancellationToken, availableVersion.DiffUri);

                    await UpgradeSchemaAsync(executingVersion, script, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is SchemaManagerException || ex is InvalidOperationException)
            {
                _logger.LogError(ex, ex.Message);
                return;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, string.Format(Resources.RequestFailedMessage, server));
                return;
            }
            catch (Exception ex)
            {
                if (ex is SqlException || ex is ExecutionFailureException)
                {
                    _logger.LogError(ex, string.Format(Resources.QueryExecutionErrorMessage, ex.Message));
                    return;
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IList<AvailableVersion>> GetAvailableSchema(Uri server, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(server, nameof(server));

            _schemaClient.SetUri(server);

            try
            {
                List<AvailableVersion> availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken);

                // To ensure that schema version null/0 is not printed
                if (availableVersions.First().Id == 0)
                {
                    availableVersions.RemoveAt(0);
                }

                return availableVersions;
            }
            catch (SchemaManagerException ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, string.Format(Resources.RequestFailedMessage, server));
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IList<CurrentVersion>> GetCurrentSchema(string connectionString, Uri server, CancellationToken cancellationToken = default)
        {
            EnsureArg.IsNotNull(server, nameof(server));
            EnsureArg.IsNotNullOrEmpty(connectionString, nameof(connectionString));

            try
            {
                _schemaClient.SetUri(server);
                _sqlServerDataStoreConfiguration.ConnectionString = connectionString;

                // Base schema is required to run the schema migration tool.
                // This method also initializes the database if not initialized yet.
                await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(cancellationToken);

                // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
                // since the Schema job polls and upserts at the specified interval in the service.
                await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(cancellationToken);

                return await _schemaClient.GetCurrentVersionInformationAsync(cancellationToken);
            }
            catch (SchemaManagerException ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, string.Format(Resources.RequestFailedMessage, server));
                throw;
            }
        }

        private async Task<List<AvailableVersion>> FetchUpdatedAvailableVersionsAsync(CancellationToken cancellationToken)
        {
            var availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken);

            availableVersions.Sort((x, y) => x.Id.CompareTo(y.Id));

            if (availableVersions.First().Id != await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken))
            {
                throw new SchemaManagerException(Resources.AvailableVersionsErrorMessage);
            }

            return availableVersions;
        }

        private async Task ValidateVersionCompatibility(int maxAvailableVersion, CancellationToken cancellationToken)
        {
            CompatibleVersion compatibleVersion = await _schemaClient.GetCompatibilityAsync(cancellationToken);

            if (maxAvailableVersion > compatibleVersion.Max)
            {
                throw new SchemaManagerException(string.Format(Resources.VersionIncompatibilityMessage, maxAvailableVersion));
            }
        }

        private async Task<string> GetScriptAsync(int version, string scriptUri, CancellationToken cancellationToken, string diffUri = null)
        {
            if (version == 1)
            {
                return await _schemaClient.GetScriptAsync(new Uri(scriptUri, UriKind.Relative), cancellationToken);
            }

            return await _schemaClient.GetDiffScriptAsync(new Uri(diffUri, UriKind.Relative), cancellationToken);
        }

        private async Task UpgradeSchemaAsync(int version, string script, CancellationToken cancellationToken)
        {
            // check if the record for given version exists in failed status
            await _schemaManagerDataStore.DeleteSchemaVersionAsync(version, SchemaVersionStatus.Failed.ToString(), cancellationToken);

            await _schemaManagerDataStore.ExecuteScriptAndCompleteSchemaVersionAsync(script, version, cancellationToken);

            _logger.LogInformation(string.Format(Resources.SchemaMigrationSuccessMessage, version));
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

        private async Task ExecutePaasSchemaIfExistsAsync(CancellationToken cancellationToken)
        {
            List<PaasSchema> paasSchemas = await _schemaClient.GetPaasScriptAsync(cancellationToken);

            // For OSS, the paas scripts would not be present so return if paas schema is null.
            if (paasSchemas == null || paasSchemas.Count < 1)
            {
                return;
            }

            _logger.LogInformation(Resources.CreatePaasSchemaVersionTableMessage);
            await _schemaManagerDataStore.CreatePaasSchemaTableIfNotExistsAsync(cancellationToken);

            foreach (var paasSchema in paasSchemas)
            {
                int version = paasSchema.Id;

                if (await _schemaManagerDataStore.ExistsPaasSchemaRecordAsync(version, SchemaVersionStatus.Completed.ToString(), cancellationToken))
                {
                    _logger.LogInformation(string.Format(Resources.PaasSchemaAlreadyExists, version));
                    continue;
                }

                // Delete the versioned record if exists in failed status
                await _schemaManagerDataStore.DeletesPaasSchemaFailedRecordAsync(version, cancellationToken);

                _logger.LogInformation(string.Format(Resources.ApplyPaasSchemaStarted, version));

                await _schemaManagerDataStore.ExecuteScriptAndCompleteSchemaVersionAsync(paasSchema.ScriptContent, version, cancellationToken, true);

                _logger.LogInformation(Resources.ApplyPaasSchemaCompleted);
            }
        }
    }
}
