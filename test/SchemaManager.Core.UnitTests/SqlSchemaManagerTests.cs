// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;
using NSubstitute;
using SchemaManager.Core.Model;
using Xunit;

namespace SchemaManager.Core.UnitTests
{
    public class SqlSchemaManagerTests
    {
        private readonly SqlSchemaManager _sqlSchemaManager;
        private readonly SqlServerDataStoreConfiguration _configuration;
        private readonly ISchemaManagerDataStore _schemaManagerDataStore = Substitute.For<ISchemaManagerDataStore>();
        private readonly ISchemaClient _client = Substitute.For<ISchemaClient>();
        private readonly IBaseSchemaRunner _baseSchemaRunner = Substitute.For<IBaseSchemaRunner>();

        public SqlSchemaManagerTests()
        {
            _configuration = new SqlServerDataStoreConfiguration
            {
                ConnectionString = string.Empty,
            };

            _baseSchemaRunner.EnsureBaseSchemaExistsAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
            _baseSchemaRunner.EnsureInstanceSchemaRecordExistsAsync(default).ReturnsForAnyArgs(Task.FromResult(true));
            _sqlSchemaManager = new SqlSchemaManager(_configuration, _baseSchemaRunner, _schemaManagerDataStore, _client, NullLogger<SqlSchemaManager>.Instance);
        }

        [Fact]
        public async Task GetCurrentSchema_OneSchema_Succeeds()
        {
            _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { new CurrentVersion(1, "Complete", new List<string> { "server1" }) });

            var current = await _sqlSchemaManager.GetCurrentSchema("connectionString", new Uri("https://localhost/"));

            Assert.NotNull(current);
            Assert.Single(current);
            Assert.Equal(1, current[0].Id);
            await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureBaseSchemaExistsAsync(default);
            await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureInstanceSchemaRecordExistsAsync(default);
        }

        [Fact]
        public async Task GetCurrentSchema_EmptyList_Succeeds()
        {
            _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { });

            var current = await _sqlSchemaManager.GetCurrentSchema("connectionString", new Uri("https://localhost/"));

            Assert.NotNull(current);
            Assert.Empty(current);
            await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureBaseSchemaExistsAsync(default);
            await _baseSchemaRunner.ReceivedWithAnyArgs().EnsureInstanceSchemaRecordExistsAsync(default);
        }

        [Fact]
        public async Task GetAvailableSchema_SingleList_Succeeds()
        {
            _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql") });
            var available = await _sqlSchemaManager.GetAvailableSchema(new Uri("https://localhost/"));

            Assert.NotNull(available);
            Assert.Single(available);
            Assert.Equal(1, available[0].Id);
            Assert.Equal("_script/1.sql", available[0].ScriptUri);
            Assert.Equal("_script/1.diff.sql", available[0].DiffUri);
        }

        [Fact]
        public async Task GetAvailableSchema_ContainsVersionZero_RemovesZero()
        {
            _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(0, "_script/0.sql", "_script/0.diff.sql"), new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql") });
            var available = await _sqlSchemaManager.GetAvailableSchema(new Uri("https://localhost/"));

            Assert.NotNull(available);
            Assert.Single(available);
            Assert.Equal(1, available[0].Id);
            Assert.Equal("_script/1.sql", available[0].ScriptUri);
            Assert.Equal("_script/1.diff.sql", available[0].DiffUri);
        }

        [Fact]
        public async Task ApplySchema_UsingDiffScript_Succeeds()
        {
            _schemaManagerDataStore.GetCurrentSchemaVersionAsync(default).ReturnsForAnyArgs(Task.FromResult(1));
            _client.GetCurrentVersionInformationAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<CurrentVersion> { });
            _client.GetAvailabilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new List<AvailableVersion> { new AvailableVersion(1, "_script/1.sql", "_script/1.diff.sql"), new AvailableVersion(2, "_script/2.sql", "_script/2.diff.sql") });
            _client.GetCompatibilityAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new CompatibleVersion(1, 2));
            _client.GetDiffScriptAsync(Arg.Is<Uri>(new Uri("_script/2.diff.sql", UriKind.Relative)), Arg.Any<CancellationToken>()).Returns("script");
            await _sqlSchemaManager.ApplySchema("connectionString", new Uri("https://localhost/"), new MutuallyExclusiveType { Latest = false, Version = 2, Next = true });
            await _schemaManagerDataStore.Received().ExecuteScriptAndCompleteSchemaVersionAsync(Arg.Is("script"), Arg.Is(2), Arg.Any<CancellationToken>());
        }
    }
}
