// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema.Manager
{
    public class BaseSchemaRunnerTests : SqlIntegrationTestBase
    {
        private readonly BaseSchemaRunner _runner;
        private readonly ISchemaManagerDataStore _dataStore;

        public BaseSchemaRunnerTests(ITestOutputHelper output)
            : base(output)
        {
            var sqlConnectionFactory = new DefaultSqlConnectionFactory(ConnectionStringProvider);
            var config = Options.Create(new SqlServerDataStoreConfiguration());
            _dataStore = new SchemaManagerDataStore(sqlConnectionFactory, config, NullLogger<SchemaManagerDataStore>.Instance);

            _runner = new BaseSchemaRunner(sqlConnectionFactory, _dataStore, ConnectionStringProvider, NullLogger<BaseSchemaRunner>.Instance);
        }

        [Fact]
        public async Task EnsureBaseSchemaExist_DoesNotExist_CreatesIt()
        {
            Assert.False(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
            await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
            Assert.True(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        }

        [Fact]
        public async Task EnsureBaseSchemaExist_Exists_DoesNothing()
        {
            Assert.False(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
            await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
            Assert.True(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
            await _runner.EnsureBaseSchemaExistsAsync(CancellationToken.None);
            Assert.True(await _dataStore.BaseSchemaExistsAsync(CancellationToken.None));
        }

        [Fact]
        public async Task EnsureInstanceSchemaRecordExists_WhenNotExists_Throws()
        {
            await Assert.ThrowsAsync<SchemaManagerException>(() => _runner.EnsureInstanceSchemaRecordExistsAsync(CancellationToken.None));
        }
    }
}
