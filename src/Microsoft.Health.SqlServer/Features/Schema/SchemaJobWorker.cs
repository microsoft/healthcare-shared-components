// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core;
using Microsoft.Health.Core.Features.Control;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Extensions;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// The worker responsible for running the schema job.
/// It inserts the instance schema information.
/// It polls the specified time to update the instance schema information and deletes the expired instance schema information, if any.
/// </summary>
public class SchemaJobWorker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
    private readonly IMediator _mediator;
    private readonly IProcessTerminator _processTerminator;
    private readonly ILogger _logger;

    public SchemaJobWorker(
        IServiceProvider services,
        IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration,
        IMediator mediator,
        IProcessTerminator processTerminator,
        ILogger<SchemaJobWorker> logger)
    {
        EnsureArg.IsNotNull(services, nameof(services));
        EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
        EnsureArg.IsNotNull(mediator, nameof(mediator));
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(processTerminator, nameof(processTerminator));

        _serviceProvider = services;
        _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration.Value;
        _mediator = mediator;
        _processTerminator = processTerminator;
        _logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Continue trying despite errors.")]
    public async Task ExecuteAsync(SchemaInformation schemaInformation, string instanceName, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));

        _logger.LogInformation("Polling started at {UtcTime}", Clock.UtcNow);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                ISchemaDataStore schemaDataStore = scope.ServiceProvider.GetRequiredService<ISchemaDataStore>();

                int? previous = schemaInformation.Current;
                schemaInformation.Current = await schemaDataStore.UpsertInstanceSchemaInformationAsync(instanceName, schemaInformation, cancellationToken).ConfigureAwait(false);

                // If there was a change in the schema version and this isn't the base schema
                if (schemaInformation.Current != previous && schemaInformation.Current > 0)
                {
                    var isFullSchemaSnapshot = previous == 0;

                    await _mediator.NotifySchemaUpgradedAsync((int)schemaInformation.Current, isFullSchemaSnapshot).ConfigureAwait(false);
                }

                await schemaDataStore.DeleteExpiredInstanceSchemaAsync(cancellationToken).ConfigureAwait(false);

                if (_sqlServerDataStoreConfiguration.TerminateWhenSchemaVersionUpdatedTo.HasValue && _sqlServerDataStoreConfiguration.TerminateWhenSchemaVersionUpdatedTo.Value == schemaInformation.Current)
                {
                    _processTerminator.Terminate(cancellationToken);
                }
            }
            catch (SqlException se) when (se.Number == SqlErrorCodes.CouldNotFoundStoredProc && schemaInformation.Current == null)
            {
                // this could happen during schema initialization until base schema is not executed so can be ignored
            }
            catch (Exception ex)
            {
                // The job failed.
                _logger.LogError(ex, "Unhandled exception in the worker.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_sqlServerDataStoreConfiguration.SchemaOptions.JobPollingFrequencyInSeconds), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancel requested.
                break;
            }
        }
    }
}
