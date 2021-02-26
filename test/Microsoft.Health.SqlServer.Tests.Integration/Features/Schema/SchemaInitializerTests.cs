// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema
{
    public class SchemaInitializerTests : SqlIntegrationTestBase
    {
        public SchemaInitializerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async Task DoesDatabaseExist_DoesNotExist_ReturnsFalse()
        {
            Assert.False(await SchemaInitializer.DoesDatabaseExistAsync(Connection, "doesnotexist", CancellationToken.None));
        }

        [Fact]
        public async Task DoesDataBaseExist_Exists_ReturnsTrue()
        {
            const string dbName = "willexist";

            try
            {
                Assert.False(await SchemaInitializer.DoesDatabaseExistAsync(Connection, dbName, CancellationToken.None));
                Assert.True(await SchemaInitializer.CreateDatabaseAsync(Connection, dbName, CancellationToken.None));
                Assert.True(await SchemaInitializer.DoesDatabaseExistAsync(Connection, dbName, CancellationToken.None));
            }
            finally
            {
                await DeleteDatabaseAsync(dbName);
            }
        }
    }
}
