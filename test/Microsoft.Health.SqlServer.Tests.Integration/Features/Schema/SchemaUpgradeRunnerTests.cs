// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.SqlServer.Management.Common;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema
{
    public class SchemaUpgradeRunnerTests : SqlIntegrationTestBase
    {
        private SchemaUpgradeRunner _runner;
        private SchemaManagerDataStore _schemaDataStore;

        public SchemaUpgradeRunnerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var connectionFactory = Substitute.For<ISqlConnectionFactory>();
            connectionFactory.GetSqlConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs((x) => GetSqlConnection());
            _schemaDataStore = new SchemaManagerDataStore(connectionFactory);
            _runner = new SchemaUpgradeRunner(new ScriptProvider<SchemaVersion>(), new BaseScriptProvider(), NullLogger<SchemaUpgradeRunner>.Instance, connectionFactory, _schemaDataStore);
        }

        [Fact]
        public async Task ApplyBaseSchema_DoesNotExist_Succeeds()
        {
            Assert.False(await _schemaDataStore.BaseSchemaExistsAsync(CancellationToken.None));
            await _runner.ApplyBaseSchemaAsync(CancellationToken.None);
            Assert.True(await _schemaDataStore.BaseSchemaExistsAsync(CancellationToken.None));
        }

        [Fact]
        public async Task ApplySchema_BaseSchemaDoesNotExist_Fails()
        {
            Assert.False(await _schemaDataStore.BaseSchemaExistsAsync(CancellationToken.None));
            var outerException = await Assert.ThrowsAsync<ExecutionFailureException>(() => _runner.ApplySchemaAsync(1, true, CancellationToken.None));
            Assert.Contains("Invalid object name 'dbo.SchemaVersion'", outerException.InnerException.Message);
        }

        [Fact]
        public async Task ApplySchema_BaseSchemaExists_Succeeds()
        {
            await _runner.ApplyBaseSchemaAsync(CancellationToken.None);
            await _runner.ApplySchemaAsync(1, applyFullSchemaSnapshot: true, CancellationToken.None);
            var version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None);
            Assert.Equal(1, version);
        }

        [Fact]
        public async Task ApplySchema_UsingDiff_Succeeds()
        {
            await _runner.ApplyBaseSchemaAsync(CancellationToken.None);
            await _runner.ApplySchemaAsync(2, applyFullSchemaSnapshot: true, CancellationToken.None);
            var version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None);
            Assert.Equal(2, version);
            await _runner.ApplySchemaAsync(3, applyFullSchemaSnapshot: false, CancellationToken.None);
            version = await _schemaDataStore.GetCurrentSchemaVersionAsync(CancellationToken.None);
            Assert.Equal(3, version);
        }
    }
}
