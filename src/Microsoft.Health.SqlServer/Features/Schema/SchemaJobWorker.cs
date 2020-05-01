// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer.Features.Schema
{
    /// <summary>
    /// The worker responsible for running the schema job.
    /// </summary>
    public class SchemaJobWorker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly ILogger _logger;

        public SchemaJobWorker(IServiceProvider services, SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration, ILogger<SchemaJobWorker> logger)
        {
            EnsureArg.IsNotNull(services, nameof(services));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _serviceProvider = services;
            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration;
            _logger = logger;
        }

        public async Task ExecuteAsync(SchemaInformation schemaInformation, string instanceName, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Polling started at {Clock.UtcNow}");

            using (var scope = _serviceProvider.CreateScope())
            {
                var schemaDataStore = scope.ServiceProvider.GetRequiredService<ISchemaDataStore>();

                // Ensure schemaInformation has the latest current version
                schemaInformation.Current = await schemaDataStore.UpsertInstanceSchemaInformationAsync(instanceName, schemaInformation, cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var schemaDataStore = scope.ServiceProvider.GetRequiredService<ISchemaDataStore>();

                        await Task.Delay(TimeSpan.FromSeconds(_sqlServerDataStoreConfiguration.SchemaOptions.JobPollingFrequencyInSeconds), cancellationToken);

                        schemaInformation.Current = await schemaDataStore.UpsertInstanceSchemaInformationAsync(instanceName, schemaInformation, cancellationToken);

                        await schemaDataStore.DeleteExpiredInstanceSchemaAsync();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancel requested.
                    break;
                }
                catch (Exception ex)
                {
                    // The job failed.
                    _logger.LogError(ex, "Unhandled exception in the worker.");
                }
            }
        }
    }
}
