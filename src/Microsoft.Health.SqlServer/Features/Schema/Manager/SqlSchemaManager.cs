// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public class SqlSchemaManager : ISchemaManager
{
    private readonly IBaseSchemaRunner _baseSchemaRunner;
    private readonly ISchemaManagerDataStore _schemaManagerDataStore;
    private readonly ISchemaClient _schemaClient;
    private readonly ILogger<SqlSchemaManager> _logger;
    private readonly IMediator _mediator;

    private const int RetryAttempts = 3;

    public SqlSchemaManager(
        IBaseSchemaRunner baseSchemaRunner,
        ISchemaManagerDataStore schemaManagerDataStore,
        ISchemaClient schemaClient,
        IMediator mediator,
        ILogger<SqlSchemaManager> logger)
    {
        _baseSchemaRunner = EnsureArg.IsNotNull(baseSchemaRunner, nameof(baseSchemaRunner));
        _schemaManagerDataStore = EnsureArg.IsNotNull(schemaManagerDataStore, nameof(schemaManagerDataStore));
        _schemaClient = EnsureArg.IsNotNull(schemaClient, nameof(schemaClient));
        _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    internal TimeSpan RetrySleepDuration { get; set; } = TimeSpan.FromSeconds(20);

    /// <inheritdoc />
    public virtual async Task ApplySchema(MutuallyExclusiveType type, bool force = false, CancellationToken token = default)
    {
        EnsureArg.IsNotNull(type, nameof(type));

        try
        {
            // Base schema is required to run the schema migration tool.
            // This method also initializes the database if not initialized yet.
            await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(token).ConfigureAwait(false);

            // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
            // since the Schema job polls and upserts at the specified interval in the service.
            await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(token).ConfigureAwait(false);

            int retryCountForHttpRequestException = 18;

            _ = await Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: retryCountForHttpRequestException,   // approx. 3 mins wait time for the service to responds to requests
                    sleepDurationProvider: retryCount => TimeSpan.FromSeconds(10),
                    onRetry: (exception, sleepDuration, retryCount, context) =>
                        _logger.LogWarning(exception, "Attempt {Attempt} of {MaxAttempts} to wait for the server to get started.", retryCount, retryCountForHttpRequestException))
                .ExecuteAsync(GetAvailableSchema, token)
                .ConfigureAwait(false);

            // If the user hits apply command multiple times in a row, then the service schema job might not poll the updated available versions
            // so there are retries to give it a fair amount of time.

            List<AvailableVersion> availableVersions = await Policy.Handle<SchemaManagerException>()
                .WaitAndRetryAsync(
                    retryCount: RetryAttempts,
                    sleepDurationProvider: retryCount => RetrySleepDuration,
                    onRetry: (exception, sleepDuration, retryCount, context) =>
                        _logger.LogWarning(exception, "Attempt {Attempt} of {MaxAttempts} to wait for the current version to be updated on the server.", retryCount, RetryAttempts))
                .ExecuteAsync(FetchUpdatedAvailableVersionsAsync, token)
                .ConfigureAwait(false);

            if (availableVersions == null || availableVersions.Count == 0 || availableVersions.Count == 1)
            {
                _logger.LogInformation("There are no available versions.");
                return;
            }

            // Removes the current version since the first available version is always the current version which is already applied.
            availableVersions.RemoveAt(0);

            var targetVersion = type.Next ?
                availableVersions[0].Id :
                (type.Latest ? availableVersions[^1].Id : type.Version);
            if (availableVersions[0].Id > targetVersion)
            {
                _logger.LogError("The current schema version is already greater than or equals to the target schema version.");
                return;
            }

            availableVersions = availableVersions.Where(availableVersion => availableVersion.Id <= targetVersion)
                .ToList();

            // Checking the specified version is not out of range of available versions
            if (availableVersions.Count < 1 || targetVersion < availableVersions[0].Id || targetVersion > availableVersions[^1].Id)
            {
                throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, FormatResources.SpecifiedVersionNotAvailable, targetVersion));
            }


            // TTL in instance schema table can cause this call to fail on first attempt so allow for retries
            await Policy.Handle<SchemaManagerException>()
                .WaitAndRetryAsync(
                    retryCount: RetryAttempts,
                    sleepDurationProvider: retryCount => TimeSpan.FromSeconds(60),
                    onRetry: (exception, sleepDuration, retryCount, context) =>
                        _logger.LogInformation(exception, "Attempt {Attempt} of {MaxAttempts} of validating version compatiblity", retryCount, RetryAttempts))
                .ExecuteAsync(t => ValidateVersionCompatibility(availableVersions[^1].Id, t), token)
                .ConfigureAwait(false);

            if (availableVersions[0].Id == 1)
            {
                // Upgrade schema directly to the latest schema version
                _logger.LogInformation("Schema migration is started for the version : {Version}.", availableVersions[^1].Id);

                string script = await _schemaClient.GetScriptAsync(availableVersions[^1].Id, token).ConfigureAwait(false);

                // full schema is not ran hence above script contains full schema -> applyFullSchemaSnapshot = true
                await ApplySchemaInternalAsync(availableVersions[^1].Id, script, applyFullSchemaSnapshot: true, token).ConfigureAwait(false);
                return;
            }

            foreach (AvailableVersion availableVersion in availableVersions)
            {
                int executingVersion = availableVersion.Id;

                _logger.LogInformation("Schema migration is started for the version : {Version}.", executingVersion);

                // Given that the service is not running (meaning SchemaJobWorker is also not running which is responsible to update CurrentVersion in InstanceSchema table)
                // If the current schemaVersion is set to x and we want to upgrade it to version x+5 then after applying first version x+1 we will start getting an error to verify if all the instances are running the previous version
                // This could happen for services that are in Warned or Suspended state. Forcing the apply schema by skipping the ValidateInstancesVersionAsync
                if (!force)
                {
                    await Policy.Handle<SchemaManagerException>()
                        .WaitAndRetryAsync(
                            retryCount: RetryAttempts,
                            sleepDurationProvider: retryCount => RetrySleepDuration,
                            onRetry: (exception, sleepDuration, retryCount, context) =>
                                _logger.LogWarning(exception, "Attempt {Attempt} of {MaxAttempts} to verify if all the instances are running the previous version.", retryCount, RetryAttempts))
                        .ExecuteAsync(t => ValidateInstancesVersionAsync(executingVersion, t), token)
                        .ConfigureAwait(false);
                }

                string script = await _schemaClient.GetDiffScriptAsync(executingVersion, token).ConfigureAwait(false);

                // here we do have a full schema ran already so just applying migrations -> applyFullSchemaSnapshot = false
                await ApplySchemaInternalAsync(executingVersion, script, applyFullSchemaSnapshot: false, token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is SchemaManagerException || ex is InvalidOperationException)
        {
            _logger.LogError(ex, "Schema manager encountered an exception when applying schemas.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to connect to host.");
            throw;
        }
        catch (Exception ex)
        {
            if (ex is SqlException)
            {
                _logger.LogError(ex, "Script execution has failed.");
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AvailableVersion>> GetAvailableSchema(CancellationToken cancellationToken = default)
    {
        try
        {
            List<AvailableVersion> availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken).ConfigureAwait(false);

            // To ensure that schema version null/0 is not printed
            if (availableVersions != null && availableVersions.Count > 0 && availableVersions[0].Id == 0)
            {
                availableVersions.RemoveAt(0);
            }

            return availableVersions;
        }
        catch (SchemaManagerException ex)
        {
            _logger.LogError(ex, "Schema manager encountered an exception when fetching the available schemas.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to connect to host.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CurrentVersion>> GetCurrentSchema(CancellationToken cancellationToken = default)
    {
        try
        {
            // Base schema is required to run the schema migration tool.
            // This method also initializes the database if not initialized yet.
            await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(cancellationToken).ConfigureAwait(false);

            // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
            // since the Schema job polls and upserts at the specified interval in the service.
            await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(cancellationToken).ConfigureAwait(false);

            return await _schemaClient.GetCurrentVersionInformationAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SchemaManagerException ex)
        {
            _logger.LogError(ex, "Schema manager encountered an exception when resolving the current schema.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to connect to host.");
            throw;
        }
    }

    private async Task<List<AvailableVersion>> FetchUpdatedAvailableVersionsAsync(CancellationToken cancellationToken)
    {
        List<AvailableVersion> availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken).ConfigureAwait(false);

        if (availableVersions == null || availableVersions.Count == 0)
        {
            return availableVersions;
        }

        availableVersions.Sort((x, y) => x.Id.CompareTo(y.Id));

        if (availableVersions[0].Id != await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new SchemaManagerException(Resources.AvailableVersionsErrorMessage);
        }

        return availableVersions;
    }

    private async Task ValidateVersionCompatibility(int maxAvailableVersion, CancellationToken cancellationToken)
    {
        CompatibleVersion compatibleVersion = await _schemaClient.GetCompatibilityAsync(cancellationToken).ConfigureAwait(false);

        if (maxAvailableVersion > compatibleVersion.Max)
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, FormatResources.VersionIncompatibilityMessage, maxAvailableVersion));
        }
    }

    private async Task ApplySchemaInternalAsync(int version, string script, bool applyFullSchemaSnapshot, CancellationToken cancellationToken)
    {
        // check if the record for given version exists in failed status
        await _schemaManagerDataStore.DeleteSchemaVersionAsync(version, SchemaVersionStatus.failed.ToString(), cancellationToken).ConfigureAwait(false);

        await _schemaManagerDataStore.ExecuteScriptAndCompleteSchemaVersionAsync(script, version, applyFullSchemaSnapshot, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Schema migration completed successfully for the version : {Version}.", version);

        // It is to publish the SchemaUpgraded event to notify the service that schema initialization or upgrade is completed to this version
        // for e.g. fhir service listents to this event and initialize its dictionaries after schema is initialized.
        await _mediator.NotifySchemaUpgradedAsync(version, applyFullSchemaSnapshot).ConfigureAwait(false);
        _logger.LogInformation("Schema upgrade notification sent for version: {Version}, applyFullSchemaSnapshot: {ApplyFullSchemaSnapshot}", version, applyFullSchemaSnapshot);
    }

    private async Task ValidateInstancesVersionAsync(int version, CancellationToken cancellationToken)
    {
        List<CurrentVersion> currentVersions = await _schemaClient.GetCurrentVersionInformationAsync(cancellationToken).ConfigureAwait(false);

        // check if any instance is not running on the previous version
        if (currentVersions.Any(currentVersion => currentVersion.Id != (version - 1) && currentVersion.Servers.Count > 0))
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, FormatResources.InvalidVersionMessage, version));
        }
    }

    public async Task<int> GetLatestSchema(CancellationToken cancellationToken = default)
    {
        int latestVersion = await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Latest schema version in db is : {Version}", latestVersion);
        return latestVersion;
    }
}
