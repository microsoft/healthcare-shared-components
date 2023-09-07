// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public class BaseSchemaRunner : IBaseSchemaRunner
{
    private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(20);
    private const int RetryAttempts = 3;

    private readonly SqlConnectionWrapperFactory _sqlConnectionFactory;
    private readonly ISchemaManagerDataStore _schemaManagerDataStore;
    private readonly ILogger<BaseSchemaRunner> _logger;

    public BaseSchemaRunner(
        SqlConnectionWrapperFactory sqlConnectionFactory,
        ISchemaManagerDataStore schemaManagerDataStore,
        ILogger<BaseSchemaRunner> logger)
    {
        _sqlConnectionFactory = EnsureArg.IsNotNull(sqlConnectionFactory);
        _schemaManagerDataStore = EnsureArg.IsNotNull(schemaManagerDataStore);
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task EnsureBaseSchemaExistsAsync(CancellationToken cancellationToken)
    {
        IBaseScriptProvider baseScriptProvider = new BaseScriptProvider();

        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        if (!await _schemaManagerDataStore.BaseSchemaExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            var script = baseScriptProvider.GetScript();

            _logger.LogInformation("The base schema execution is started.");

            await _schemaManagerDataStore.ExecuteScriptAsync(script, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("The base schema execution is completed.");
        }
        else
        {
            _logger.LogWarning("The base schema already exists.");
        }
    }

    public async Task EnsureInstanceSchemaRecordExistsAsync(CancellationToken cancellationToken)
    {
        // Ensure that the current version record is inserted into InstanceSchema table

        await Policy.Handle<SchemaManagerException>()
            .WaitAndRetryAsync(
                retryCount: RetryAttempts,
                sleepDurationProvider: retryCount => RetrySleepDuration,
                onRetry: (exception, sleepDuration, retryCount, context) =>
                    _logger.LogWarning(exception, "Attempt {Attempt} of {MaxAttempts} to verify if the base schema is synced up with the service.", retryCount, RetryAttempts))
            .ExecuteAsync(InstanceSchemaRecordCreatedAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task InstanceSchemaRecordCreatedAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _schemaManagerDataStore.InstanceSchemaRecordExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InstanceSchemaNotFoundException(Resources.InstanceSchemaRecordErrorMessage);
            }
        }
        catch (SqlException e) when (e.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            // Table doesn't exist
            throw new SchemaManagerException(Resources.InstanceSchemaRecordTableNotFound, e);
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // The database to initialize is based off of the initial catalog
        string databaseName = SchemaInitializer.ValidateDatabaseName(_sqlConnectionFactory.DefaultDatabase);

        await CreateDatabaseIfNotExists(databaseName, cancellationToken).ConfigureAwait(false);

        bool canInitialize = false;

        // now switch to the target database
        using (SqlConnectionWrapper connection = await _sqlConnectionFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            canInitialize = await SchemaInitializer.CheckDatabasePermissionsAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        if (!canInitialize)
        {
            throw new SchemaManagerException(Resources.InsufficientTablesPermissionsMessage);
        }
    }

    private async Task CreateDatabaseIfNotExists(string databaseName, CancellationToken cancellationToken)
    {
        using SqlConnectionWrapper connection = await _sqlConnectionFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        bool doesDatabaseExist = await SchemaInitializer.DoesDatabaseExistAsync(connection, databaseName, cancellationToken).ConfigureAwait(false);
        if (!doesDatabaseExist)
        {
            _logger.LogInformation("The database does not exists.");

            bool created = await SchemaInitializer.CreateDatabaseAsync(connection, databaseName, cancellationToken).ConfigureAwait(false);

            if (created)
            {
                _logger.LogInformation("The database is created.");
            }
            else
            {
                throw new SchemaManagerException(Resources.InsufficientDatabasePermissionsMessage);
            }
        }
    }
}
