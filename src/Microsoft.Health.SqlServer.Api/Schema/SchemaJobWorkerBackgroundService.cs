// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;

namespace Microsoft.Health.SqlServer.Api.Features.Schema
{
    /// <summary>
    /// The background service used to host the <see cref="SchemaJobWorker"/>.
    /// </summary>
    public class SchemaJobWorkerBackgroundService : BackgroundService
    {
        private readonly string _instanceName;
        private readonly SchemaJobWorker _schemaJobWorker;
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
        private readonly SchemaInformation _schemaInformation;

        public SchemaJobWorkerBackgroundService(SchemaJobWorker schemaJobWorker, IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration, SchemaInformation schemaInformation)
        {
            EnsureArg.IsNotNull(schemaJobWorker, nameof(schemaJobWorker));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));

            _schemaJobWorker = schemaJobWorker;
            _sqlServerDataStoreConfiguration = sqlServerDataStoreConfiguration.Value;
            _schemaInformation = schemaInformation;
#if NET5_0_OR_GREATER
            _instanceName = Guid.NewGuid() + "-" + Environment.ProcessId;
#else
            _instanceName = Guid.NewGuid() + "-" + System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
             await _schemaJobWorker.ExecuteAsync(_schemaInformation, _instanceName, stoppingToken).ConfigureAwait(false);
        }
    }
}
