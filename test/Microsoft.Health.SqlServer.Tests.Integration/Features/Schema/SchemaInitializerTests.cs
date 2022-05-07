// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema;

public class SchemaInitializerTests : SqlIntegrationTestBase
{
    public SchemaInitializerTests(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    [Fact]
    public async Task InvalidDatabaseName_CreateDatabaseAsync_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, "[something] DROP DATABASE Production --", CancellationToken.None));
    }

    [Fact]
    public async Task DatabaseDoesNotExist_DoesDatabaseExistAsync_ReturnsFalse()
    {
        Assert.False(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, "doesnotexist", CancellationToken.None));
    }

    [Fact]
    public async Task DatabaseExists_DoesDatabaseExistAsync_ReturnsTrue()
    {
        const string dbName = "willexist";

        try
        {
            Assert.False(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, dbName, CancellationToken.None));
            Assert.True(await SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, dbName, CancellationToken.None));
            Assert.True(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, dbName, CancellationToken.None));
        }
        finally
        {
            await DeleteDatabaseAsync(dbName);
        }
    }
}
