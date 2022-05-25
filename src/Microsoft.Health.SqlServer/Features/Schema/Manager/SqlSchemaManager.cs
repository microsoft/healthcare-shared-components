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
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using Microsoft.SqlServer.Management.Common;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public class SqlSchemaManager : ISchemaManager
{
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
    private readonly IBaseSchemaRunner _baseSchemaRunner;
    private readonly ISchemaManagerDataStore _schemaManagerDataStore;
    private readonly ISchemaClient _schemaClient;
    private readonly ILogger<SqlSchemaManager> _logger;
    private readonly IMediator _mediator;

    private TimeSpan _retrySleepDuration = TimeSpan.FromSeconds(20);
    private const int RetryAttempts = 3;

    public SqlSchemaManager(
        IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration,
        IBaseSchemaRunner baseSchemaRunner,
        ISchemaManagerDataStore schemaManagerDataStore,
        ISchemaClient schemaClient,
        IMediator mediator,
        ILogger<SqlSchemaManager> logger)
    {
        _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
        _baseSchemaRunner = EnsureArg.IsNotNull(baseSchemaRunner, nameof(baseSchemaRunner));
        _schemaManagerDataStore = EnsureArg.IsNotNull(schemaManagerDataStore, nameof(schemaManagerDataStore));
        _schemaClient = EnsureArg.IsNotNull(schemaClient, nameof(schemaClient));
        _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    public virtual async Task ApplySchema(string connectionString, Uri server, MutuallyExclusiveType type, CancellationToken token = default)
    {
        EnsureArg.IsNotNullOrEmpty(connectionString, nameof(connectionString));
        EnsureArg.IsNotNull(server, nameof(server));
        EnsureArg.IsNotNull(type, nameof(type));

        _schemaClient.SetUri(server);

        try
        {
            _sqlServerDataStoreConfiguration.ConnectionString = connectionString;

            // Base schema is required to run the schema migration tool.
            // This method also initializes the database if not initialized yet.
            await _baseSchemaRunner.EnsureBaseSchemaExistsAsync(token).ConfigureAwait(false);

            // If InstanceSchema table is just created(as part of baseSchema), it takes a while to insert a version record
            // since the Schema job polls and upserts at the specified interval in the service.
            await _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(token).ConfigureAwait(false);

            int attemptCount = 1;
            int retryCountForHttpRequestException = 18;

            var availableVersions = await Policy.Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: retryCountForHttpRequestException,   // approx. 3 mins wait time for the service to responds to requests
                sleepDurationProvider: (retryCount) => TimeSpan.FromSeconds(10),
                onRetry: (exception, retryCount) =>
                {
                    _logger.LogError(exception, "Attempt {Attempt} of {MaxAttempts} to wait for the server to get started.", attemptCount++, retryCountForHttpRequestException);
                })
            .ExecuteAsync(token => GetAvailableSchema(server, token), token)
            .ConfigureAwait(false);

            // If the user hits apply command multiple times in a row, then the service schema job might not poll the updated available versions
            // so there are retries to give it a fair amount of time.
            attemptCount = 1;

            availableVersions = await Policy.Handle<SchemaManagerException>()
            .WaitAndRetryAsync(
                retryCount: RetryAttempts,
                sleepDurationProvider: (retryCount) => _retrySleepDuration,
                onRetry: (exception, retryCount) =>
                {
                    _logger.LogError(exception, "Attempt {Attempt} of {MaxAttempts} to wait for the current version to be updated on the server.", attemptCount++, RetryAttempts);
                })
            .ExecuteAsync(token => FetchUpdatedAvailableVersionsAsync(token), token)
            .ConfigureAwait(false);

            if (availableVersions.Count == 1)
            {
                _logger.LogError("There are no available versions.");
                return;
            }

            // Removes the current version since the first available version is always the current version which is already applied.
            availableVersions.RemoveAt(0);

            var targetVersion = type.Next ?
                availableVersions.First().Id :
                (type.Latest ? availableVersions.Last().Id : type.Version);
            if (availableVersions.First().Id > targetVersion)
            {
                _logger.LogError("The current schema version is already greater than or equals to the target schema version.");
                return;
            }

            availableVersions = availableVersions.Where(availableVersion => availableVersion.Id <= targetVersion)
                .ToList();

            // Checking the specified version is not out of range of available versions
            if (availableVersions.Count < 1 || targetVersion < availableVersions.First().Id || targetVersion > availableVersions.Last().Id)
            {
                throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.SpecifiedVersionNotAvailable, targetVersion));
            }

            await ValidateVersionCompatibility(availableVersions.Last().Id, token).ConfigureAwait(false);

            if (availableVersions.First().Id == 1)
            {
                // Upgrade schema directly to the latest schema version
                _logger.LogInformation("Schema migration is started for the version : {Version}.", availableVersions.Last().Id);

                string script = await GetScriptAsync(1, availableVersions.Last().ScriptUri, token).ConfigureAwait(false);

                // full schema is not ran hence above script contains full schema -> applyFullSchemaSnapshot = true
                await ApplySchemaInternalAsync(availableVersions.Last().Id, script, applyFullSchemaSnapshot: true, token).ConfigureAwait(false);
                return;
            }

            foreach (AvailableVersion availableVersion in availableVersions)
            {
                int executingVersion = availableVersion.Id;

                _logger.LogInformation("Schema migration is started for the version : {Version}.", executingVersion);

                attemptCount = 1;

                await Policy.Handle<SchemaManagerException>()
                    .WaitAndRetryAsync(
                        retryCount: RetryAttempts,
                        sleepDurationProvider: (retryCount) => _retrySleepDuration,
                        onRetry: (exception, delay) =>
                            _logger.LogError(exception, "Attempt {Attempt} of {MaxAttempts} to verify if all the instances are running the previous version.", attemptCount++, RetryAttempts))
                    .ExecuteAsync(token => ValidateInstancesVersionAsync(executingVersion, token), token)
                    .ConfigureAwait(false);

                string script = await GetScriptAsync(executingVersion, availableVersion.ScriptUri, token, availableVersion.DiffUri).ConfigureAwait(false);

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
            _logger.LogError(ex, "Unable to connect to host '{Server}'.", server);
            throw;
        }
        catch (Exception ex)
        {
            if (ex is SqlException || ex is ExecutionFailureException)
            {
                _logger.LogError(ex, "Script execution has failed.");
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
            List<AvailableVersion> availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken).ConfigureAwait(false);

            // To ensure that schema version null/0 is not printed
            if (availableVersions.First().Id == 0)
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
            _logger.LogError(ex, "Unable to connect to host '{Server}'.", server);
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
            _logger.LogError(ex, "Unable to connect to host '{Server}'.", server);
            throw;
        }
    }

    private async Task<List<AvailableVersion>> FetchUpdatedAvailableVersionsAsync(CancellationToken cancellationToken)
    {
        List<AvailableVersion> availableVersions = await _schemaClient.GetAvailabilityAsync(cancellationToken).ConfigureAwait(false);

        availableVersions.Sort((x, y) => x.Id.CompareTo(y.Id));

        if (availableVersions.First().Id != await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false))
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
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.VersionIncompatibilityMessage, maxAvailableVersion));
        }
    }

    private async Task<string> GetScriptAsync(int version, string scriptUri, CancellationToken cancellationToken, string diffUri = null)
    {
        if (version == 1)
        {
            return await _schemaClient.GetScriptAsync(new Uri(scriptUri, UriKind.Relative), cancellationToken).ConfigureAwait(false);
        }

        return await _schemaClient.GetDiffScriptAsync(new Uri(diffUri, UriKind.Relative), cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplySchemaInternalAsync(int version, string script, bool applyFullSchemaSnapshot, CancellationToken cancellationToken)
    {
        // check if the record for given version exists in failed status
        await _schemaManagerDataStore.DeleteSchemaVersionAsync(version, SchemaVersionStatus.failed.ToString(), cancellationToken).ConfigureAwait(false);

        await _schemaManagerDataStore.ExecuteScriptAndCompleteSchemaVersionAsync(script, version, applyFullSchemaSnapshot, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Schema migration completed successfully for the version : {Version}.", version);

        // this notification is used by the fhir service to initialized the dictionaries
        await _mediator.NotifySchemaUpgradedAsync(version, applyFullSchemaSnapshot);
        _logger.LogInformation("Schema upgrade notification sent for version: {Version}, applyFullSchemaSnapshot: {ApplyFullSchemaSnapshot}", version, applyFullSchemaSnapshot);
    }

    private async Task ValidateInstancesVersionAsync(int version, CancellationToken cancellationToken)
    {
        List<CurrentVersion> currentVersions = await _schemaClient.GetCurrentVersionInformationAsync(cancellationToken).ConfigureAwait(false);

        // check if any instance is not running on the previous version
        if (currentVersions.Any(currentVersion => currentVersion.Id != (version - 1) && currentVersion.Servers.Count > 0))
        {
            throw new SchemaManagerException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidVersionMessage, version));
        }
    }

    public async Task<int> GetLatestSchema(CancellationToken cancellationToken = default)
    {
        int latestVersion = await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken);
        _logger.LogInformation("Latest schema version in db is : {Version}", latestVersion);
        return latestVersion;
    }
}
