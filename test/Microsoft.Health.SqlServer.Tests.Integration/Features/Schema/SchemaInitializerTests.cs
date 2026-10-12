// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.Tests.Integration.Features.Schema;

[TestClass]
public class SchemaInitializerTests : SqlIntegrationTestBase
{
    public SchemaInitializerTests(TestContext testContext)
        : base(testContext)
    {
    }

    [TestMethod]
    public async Task InvalidDatabaseName_CreateDatabaseAsync_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, "[something] DROP DATABASE Production --", CancellationToken.None));
    }

    [TestMethod]
    public async Task DatabaseDoesNotExist_DoesDatabaseExistAsync_ReturnsFalse()
    {
        Assert.IsFalse(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, "doesnotexist", CancellationToken.None));
    }

    [TestMethod]
    public async Task DatabaseExists_DoesDatabaseExistAsync_ReturnsTrue()
    {
        string dbName = $"Db_{Guid.NewGuid():N}";

        try
        {
            Assert.IsFalse(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, dbName, CancellationToken.None));
            Assert.IsTrue(await SchemaInitializer.CreateDatabaseAsync(ConnectionWrapper, dbName, CancellationToken.None));
            Assert.IsTrue(await SchemaInitializer.DoesDatabaseExistAsync(ConnectionWrapper, dbName, CancellationToken.None));
        }
        finally
        {
            await DeleteDatabaseAsync(dbName);
        }
    }
}
