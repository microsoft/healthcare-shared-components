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
public sealed class SchemaDatabaseNameTests : IDisposable
{
    private const string DatabaseName = "MyDatabase";

    private readonly SqlTransactionHandler _transactionHandler = new SqlTransactionHandler();
    private readonly SqlConnectionWrapperFactory _connectionFactory;
    private readonly IOptions<SqlServerDataStoreConfiguration> _options = Options.Create(new SqlServerDataStoreConfiguration());

    public SchemaDatabaseNameTests()
    {
        ISqlConnectionBuilder connectionBuilder = Substitute.For<ISqlConnectionBuilder>();
        connectionBuilder.DefaultDatabase.Returns(DatabaseName);
        _connectionFactory = new SqlConnectionWrapperFactory(
            _transactionHandler,
            connectionBuilder,
            SqlConfigurableRetryFactory.CreateNoneRetryProvider(),
            _options);
    }

    public void Dispose()
        => _transactionHandler.Dispose();

    [TestMethod]
    public void GivenConnectionFactory_WhenGettingSchemaManagerDataStoreDatabaseName_ThenReturnsDefaultDatabase()
    {
        var dataStore = new SchemaManagerDataStore(_connectionFactory, _options, NullLogger<SchemaManagerDataStore>.Instance);

        Assert.AreEqual(DatabaseName, dataStore.DatabaseName);
    }

    [TestMethod]
    public void GivenConnectionFactory_WhenGettingSqlServerSchemaDataStoreDatabaseName_ThenReturnsDefaultDatabase()
    {
        var dataStore = new SqlServerSchemaDataStore(_connectionFactory, _options, NullLogger<SqlServerSchemaDataStore>.Instance);

        Assert.AreEqual(DatabaseName, dataStore.DatabaseName);
    }

    [TestMethod]
    public void GivenSchemaManagerDataStore_WhenGettingSchemaUpgradeRunnerDatabaseName_ThenReturnsDataStoreDatabaseName()
    {
        ISchemaManagerDataStore dataStore = Substitute.For<ISchemaManagerDataStore>();
        dataStore.DatabaseName.Returns("OtherDatabase");
        var runner = new SchemaUpgradeRunner(
            Substitute.For<IScriptProvider>(),
            Substitute.For<IBaseScriptProvider>(),
            NullLogger<SchemaUpgradeRunner>.Instance,
            _connectionFactory,
            dataStore);

        Assert.AreEqual("OtherDatabase", runner.DatabaseName);
    }
}
