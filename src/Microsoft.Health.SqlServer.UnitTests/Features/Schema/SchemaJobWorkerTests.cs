﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema
{
    public class SchemaJobWorkerTests
    {
        private readonly ISchemaDataStore _schemaDataStore = Substitute.For<ISchemaDataStore>();
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration = new SqlServerDataStoreConfiguration();
        private readonly SchemaInformation schemaInformation = new SchemaInformation(1, 2);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        private readonly SchemaJobWorker _schemaJobWorker;

        public SchemaJobWorkerTests()
        {
            var scopedSchemaDataStore = Substitute.For<IScoped<ISchemaDataStore>>();
            scopedSchemaDataStore.Value.Returns(_schemaDataStore);

            _schemaJobWorker = new SchemaJobWorker(
                () => scopedSchemaDataStore,
                _sqlServerDataStoreConfiguration,
                NullLogger<SchemaJobWorker>.Instance);

            _cancellationToken = _cancellationTokenSource.Token;
        }

        [Fact]
        public async Task GivenSchemaBackgroundJob_WhenExecuted_ThenInsertAndPollingIsExecuted()
        {
            try
            {
                _cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(1000));
                await _schemaJobWorker.ExecuteAsync(schemaInformation, "instanceName", _cancellationToken);
            }
            catch (TaskCanceledException)
            {
                await _schemaDataStore.Received().InsertInstanceSchemaInformation("instanceName", schemaInformation, _cancellationToken);
                await _schemaDataStore.Received().UpsertInstanceSchemaInformation("instanceName", schemaInformation, _cancellationToken);
                await _schemaDataStore.Received().DeleteExpiredRecords();
            }
        }
    }
}
