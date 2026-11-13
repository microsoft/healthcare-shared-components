// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema.Manager;

[TestClass]
public sealed class SchemaManagerDataStoreTests : IDisposable
{
    private readonly SqlTransactionHandler _transactionHandler = new SqlTransactionHandler();

    public void Dispose()
        => _transactionHandler.Dispose();

    [TestMethod]
    public void GivenConnectionFactory_WhenGettingDatabaseName_ThenReturnsDefaultDatabase()
    {
        ISqlConnectionBuilder connectionBuilder = Substitute.For<ISqlConnectionBuilder>();
        connectionBuilder.DefaultDatabase.Returns("MyDatabase");
        IOptions<SqlServerDataStoreConfiguration> options = Options.Create(new SqlServerDataStoreConfiguration());
        var connectionFactory = new SqlConnectionWrapperFactory(
            _transactionHandler,
            connectionBuilder,
            SqlConfigurableRetryFactory.CreateNoneRetryProvider(),
            options);
        var dataStore = new SchemaManagerDataStore(connectionFactory, options, NullLogger<SchemaManagerDataStore>.Instance);

        Assert.AreEqual("MyDatabase", dataStore.DatabaseName);
    }
}
