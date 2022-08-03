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
    private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;
    private readonly ILogger<BaseSchemaRunner> _logger;

    public BaseSchemaRunner(
        SqlConnectionWrapperFactory sqlConnectionFactory,
        ISchemaManagerDataStore schemaManagerDataStore,
        ISqlConnectionStringProvider sqlConnectionStringProvider,
        ILogger<BaseSchemaRunner> logger)
    {
        EnsureArg.IsNotNull(sqlConnectionFactory);
        EnsureArg.IsNotNull(schemaManagerDataStore);
        EnsureArg.IsNotNull(sqlConnectionStringProvider);
        EnsureArg.IsNotNull(logger, nameof(logger));

        _sqlConnectionFactory = sqlConnectionFactory;
        _schemaManagerDataStore = schemaManagerDataStore;
        _sqlConnectionStringProvider = sqlConnectionStringProvider;
        _logger = logger;
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
        int attempts = 1;

        await Policy.Handle<SchemaManagerException>()
        .WaitAndRetryAsync(
            retryCount: RetryAttempts,
            sleepDurationProvider: (retryCount) => RetrySleepDuration,
            onRetry: (exception, retryCount) =>
            {
                _logger.LogWarning(
                    exception,
                    "Attempt {Attempt} of {MaxAttempts} to verify if the base schema is synced up with the service.",
                    attempts++,
                    RetryAttempts);
            })
        .ExecuteAsync(token => InstanceSchemaRecordCreatedAsync(token), cancellationToken).ConfigureAwait(false);
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
        string sqlConnectionString = await _sqlConnectionStringProvider.GetSqlConnectionString(cancellationToken).ConfigureAwait(false);
        var configuredConnectionBuilder = new SqlConnectionStringBuilder(sqlConnectionString);
        string databaseName = configuredConnectionBuilder.InitialCatalog;

        SchemaInitializer.ValidateDatabaseName(databaseName);

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
