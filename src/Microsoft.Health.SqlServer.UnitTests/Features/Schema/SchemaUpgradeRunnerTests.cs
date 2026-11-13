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
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Health.SqlServer.UnitTests.Features.Schema;

[TestClass]
public sealed class SchemaUpgradeRunnerTests : IDisposable
{
    private readonly SqlTransactionHandler _transactionHandler = new SqlTransactionHandler();

    public void Dispose()
        => _transactionHandler.Dispose();

    [TestMethod]
    public void GivenSchemaManagerDataStore_WhenGettingDatabaseName_ThenReturnsDataStoreDatabaseName()
    {
        ISqlConnectionBuilder connectionBuilder = Substitute.For<ISqlConnectionBuilder>();
        connectionBuilder.DefaultDatabase.Returns("DefaultDatabase");
        var connectionFactory = new SqlConnectionWrapperFactory(
            _transactionHandler,
            connectionBuilder,
            SqlConfigurableRetryFactory.CreateNoneRetryProvider(),
            Options.Create(new SqlServerDataStoreConfiguration()));
        ISchemaManagerDataStore dataStore = Substitute.For<ISchemaManagerDataStore>();
        dataStore.DatabaseName.Returns("OtherDatabase");
        var runner = new SchemaUpgradeRunner(
            Substitute.For<IScriptProvider>(),
            Substitute.For<IBaseScriptProvider>(),
            NullLogger<SchemaUpgradeRunner>.Instance,
            connectionFactory,
            dataStore);

        Assert.AreEqual("OtherDatabase", runner.DatabaseName);
    }
}
