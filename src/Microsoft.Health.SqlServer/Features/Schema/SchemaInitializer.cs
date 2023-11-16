// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Medallion.Threading;
using Medallion.Threading.SqlServer;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// EXPERIMENTAL - Initializes the sql schema and brings the schema up to the min supported version.
/// The purpose of this it to enable easy scenarios during development and will likely be removed later.
/// </summary>
public sealed class SchemaInitializer : IHostedService
{
    private const string MasterDatabase = "master";
    private readonly IServiceProvider _serviceProvider;
    private readonly SqlServerDataStoreConfiguration _options;
    private readonly SchemaInformation _schemaInformation;
    private readonly ILogger<SchemaInitializer> _logger;
    private readonly IMediator _mediator;
    private bool _canCallGetCurrentSchema;
    public const string SchemaUpgradeLockName = "SchemaUpgrade";

    public SchemaInitializer(
        IServiceProvider services,
        IOptions<SqlServerDataStoreConfiguration> options,
        SchemaInformation schemaInformation,
        IMediator mediator,
        ILogger<SchemaInitializer> logger)
    {
        _serviceProvider = EnsureArg.IsNotNull(services, nameof(services));
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _schemaInformation = EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
        _mediator = EnsureArg.IsNotNull(mediator, nameof(mediator));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task InitializeAsync(bool forceIncrementalSchemaUpgrade = false, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        if (!string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            await InitializeAsync(scope, forceIncrementalSchemaUpgrade, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogCritical(
                "There was no connection string supplied. Schema initialization can not be completed.");
        }
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
        => InitializeAsync(forceIncrementalSchemaUpgrade: false, cancellationToken: cancellationToken);

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private async Task InitializeAsync(IServiceScope scope, bool forceIncrementalSchemaUpgrade = false, CancellationToken cancellationToken = default)
    {
        bool schemaUpgradedNotificationSent = false;
        SqlConnectionWrapperFactory connectionFactory = scope.ServiceProvider.GetRequiredService<SqlConnectionWrapperFactory>();
        if (!await CanInitializeAsync(connectionFactory, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Initial check of schema version is {Version}", _schemaInformation.Current?.ToString(CultureInfo.InvariantCulture) ?? "NULL");

        if (_options.SchemaOptions.AutomaticUpdatesEnabled)
        {
            SchemaUpgradeRunner _schemaUpgradeRunner = scope.ServiceProvider.GetRequiredService<SchemaUpgradeRunner>();

            using SqlConnectionWrapper sqlConnection = await connectionFactory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false);
            IDistributedLock sqlLock = new SqlDistributedLock(SchemaUpgradeLockName, sqlConnection.SqlConnection);

            try
            {
                IDistributedSynchronizationHandle lockHandle = await sqlLock.TryAcquireAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                await using (lockHandle.ConfigureAwait(false))
                {
                    if (lockHandle == null)
                    {
                        _logger.LogInformation("Schema upgrade lock was not acquired, skipping");
                        return;
                    }

                    _logger.LogInformation("Schema upgrade lock acquired");

                    // Recheck the version with lock
                    await GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Schema version is {Version}", _schemaInformation.Current?.ToString(CultureInfo.InvariantCulture) ?? "NULL");

                    // If the stored procedure to get the current schema version doesn't exist
                    if (_schemaInformation.Current == null)
                    {
                        // Apply base schema
                        await _schemaUpgradeRunner.ApplyBaseSchemaAsync(cancellationToken).ConfigureAwait(false);

                        // This is for tests purpose only
                        if (forceIncrementalSchemaUpgrade)
                        {
                            // Run version 1 and and apply .diff.sql files to upgrade the schema version.
                            await _schemaUpgradeRunner.ApplySchemaAsync(_schemaInformation.MinimumSupportedVersion, applyFullSchemaSnapshot: true, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // Apply the maximum supported version. This won't consider the .diff.sql files.
                            await _schemaUpgradeRunner.ApplySchemaAsync(_schemaInformation.MaximumSupportedVersion, applyFullSchemaSnapshot: true, cancellationToken).ConfigureAwait(false);
                        }

                        await GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false);

                        await _mediator.NotifySchemaUpgradedAsync((int)_schemaInformation.Current, true).ConfigureAwait(false);

                        schemaUpgradedNotificationSent = true;
                    }

                    // If the current schema version needs to be upgraded
                    if (_schemaInformation.Current < _schemaInformation.MaximumSupportedVersion)
                    {
                        // Apply each .diff.sql file one by one.
                        int current = _schemaInformation.Current ?? 0;
                        for (int i = current + 1; i <= _schemaInformation.MaximumSupportedVersion; i++)
                        {
                            await _schemaUpgradeRunner.ApplySchemaAsync(version: i, applyFullSchemaSnapshot: false, cancellationToken).ConfigureAwait(false);

                            // we need to ensure that the schema upgrade notification is sent after updating the _schemaInformation.Current for each upgraded version
                            await GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false);

                            await _mediator.NotifySchemaUpgradedAsync((int)_schemaInformation.Current, false).ConfigureAwait(false);
                        }

                        schemaUpgradedNotificationSent = true;
                    }
                }
            }
            catch (SqlException e) when (e.Number == SqlErrorCodes.KilledSessionState)
            {
                _logger.LogWarning("Schema upgrade lock was not acquired because the session is in the kill state, skipping");
            }

            // This exception sometimes occurs at Medallion.Threading.Internal.Data.MultiplexedConnectionLock.ReleaseAsync
            catch (SqlException e) when (e.Message.Contains("The connection is broken and recovery is not possible.", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Error occurred during multiplexed release lock");
            }
        }

        // Ensure to publish the Schema notifications even when schema is up-to date and Schema Initializer is called again (like restarting FHIR server will call this again)
        // There is a dependency on this notification in FHIR server to enable some background jobs
        if (!schemaUpgradedNotificationSent)
        {
            await _mediator.NotifySchemaUpgradedAsync((int)_schemaInformation.Current, false).ConfigureAwait(false);
        }
    }

    private async Task GetCurrentSchemaVersionAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        IReadOnlySchemaManagerDataStore _schemaManagerDataStore = scope.ServiceProvider.GetRequiredService<IReadOnlySchemaManagerDataStore>();

        if (!_canCallGetCurrentSchema)
        {
            _canCallGetCurrentSchema = await _schemaManagerDataStore.ObjectExistsAsync("SelectCurrentSchemaVersion", "P", cancellationToken).ConfigureAwait(false);
        }

        if (_canCallGetCurrentSchema)
        {
            int version = await _schemaManagerDataStore.GetCurrentSchemaVersionAsync(cancellationToken).ConfigureAwait(false);
            if (version != 0)
            {
                _schemaInformation.Current = version;
            }
            else
            {
                _logger.LogInformation("No version found. It must be new database");
            }
        }
        else
        {
            _logger.LogWarning("Could not find stored procedure - dbo.SelectCurrentSchemaVersion");
        }
    }

    private async Task<bool> CanInitializeAsync(SqlConnectionWrapperFactory factory, CancellationToken cancellationToken)
    {
        if (!_options.Initialize)
        {
            return false;
        }

        string databaseName = ValidateDatabaseName(factory.DefaultDatabase);

        if (_options.AllowDatabaseCreation)
        {
            using SqlConnectionWrapper connection = await factory.ObtainSqlConnectionWrapperAsync(MasterDatabase, cancellationToken).ConfigureAwait(false);
            bool doesDatabaseExist = await DoesDatabaseExistAsync(connection, databaseName, cancellationToken).ConfigureAwait(false);

            if (!doesDatabaseExist)
            {
                _logger.LogInformation("Database does not exist");

                bool created = await CreateDatabaseAsync(connection, databaseName, cancellationToken).ConfigureAwait(false);

                if (created)
                {
                    _logger.LogInformation("Created database");
                }
                else
                {
                    _logger.LogWarning("Insufficient permissions to create the database");
                    return false;
                }
            }
        }

        bool canInitialize = false;

        // now switch to the target database
        using (SqlConnectionWrapper connection = await factory.ObtainSqlConnectionWrapperAsync(cancellationToken).ConfigureAwait(false))
        {
            canInitialize = await CheckDatabasePermissionsAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        if (!canInitialize)
        {
            _logger.LogWarning("Insufficient permissions to create tables in the database");
        }

        return canInitialize;
    }

    public static string ValidateDatabaseName(string databaseName)
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            throw new InvalidOperationException("The initial catalog must be specified in the connection string");
        }

        if (databaseName.Equals("master", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Equals("model", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Equals("msdb", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Equals("tempdb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The initial catalog in the connection string cannot be a system database");
        }

        return databaseName;
    }

    /// <summary>
    /// Check if the database exists.
    /// </summary>
    /// <param name="sqlConnection">Sql Connection with permissions to query the system tables in order to check if the provided database exists.</param>
    /// <param name="databaseName">Database name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if database exists, else returns false.</returns>
    public static async Task<bool> DoesDatabaseExistAsync(SqlConnectionWrapper sqlConnection, string databaseName, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(sqlConnection, nameof(sqlConnection));

        using SqlCommandWrapper checkDatabaseExistsCommand = sqlConnection.CreateRetrySqlCommand();

        checkDatabaseExistsCommand.CommandText = "SELECT 1 FROM sys.databases where name = @databaseName";
        checkDatabaseExistsCommand.Parameters.AddWithValue("@databaseName", databaseName);
        if ((int?)await checkDatabaseExistsCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) == 1)
        {
            return true;
        }

        return false;
    }

    public static async Task<bool> CreateDatabaseAsync(SqlConnectionWrapper connection, string databaseName, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(connection, nameof(connection));

        if (!Identifier.IsValidDatabase(databaseName))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FormatResources.InvalidDatabaseIdentifier, databaseName), nameof(databaseName));
        }

        using SqlCommandWrapper canCreateDatabaseCommand = connection.CreateRetrySqlCommand();
        canCreateDatabaseCommand.CommandText = "SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE DATABASE'";

        if ((int)await canCreateDatabaseCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) > 0)
        {
            using SqlCommandWrapper createDatabaseCommand = connection.CreateRetrySqlCommand();
            createDatabaseCommand.CommandText = $"CREATE DATABASE {databaseName}";

            await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        else
        {
            return false;
        }
    }

    public static async Task<bool> CheckDatabasePermissionsAsync(SqlConnectionWrapper connection, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(connection, nameof(connection));

        using SqlCommandWrapper command = connection.CreateRetrySqlCommand();
        command.CommandText = "SELECT count(*) FROM fn_my_permissions (NULL, 'DATABASE') WHERE permission_name = 'CREATE TABLE'";
        return (int)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) > 0;
    }
}
