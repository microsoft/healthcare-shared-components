// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Registration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests.Registration;

[TestClass]
public class SqlServerBaseRegistrationExtensionsTests
{
    private enum ExampleVersion
    {
        V0,
        V1,
    }

    [TestMethod]
    [Obsolete("To be removed when AddSqlServerBase is deleted.")]
    public void GivenEmptyServiceCollection_WhenAddingSqlServerBase_ThenAddNewServices()
    {
        var services = new ServiceCollection();
        services.AddSqlServerBase<ExampleVersion>(null);

        Assert.IsTrue(services.ContainsScoped<ISchemaDataStore, SqlServerSchemaDataStore>());
        Assert.IsTrue(services.ContainsScoped<ITransactionHandler, SqlTransactionHandler>());
        Assert.IsTrue(services.ContainsScoped<SqlConnectionWrapperFactory>());
        Assert.IsTrue(services.ContainsScoped<SqlServerSchemaDataStore>());
        Assert.IsTrue(services.ContainsScoped<SqlTransactionHandler>());

        Assert.IsTrue(services.ContainsSingleton<BaseScriptProvider>());
        Assert.IsTrue(services.ContainsSingleton<IBaseScriptProvider, BaseScriptProvider>());
        Assert.IsTrue(services.ContainsSingleton<IHostedService, SchemaInitializer>());
        Assert.IsTrue(services.ContainsScoped<ISchemaManagerDataStore>());
        Assert.IsTrue(services.ContainsSingleton<IScriptProvider, ScriptProvider<ExampleVersion>>());
        Assert.IsTrue(services.ContainsSingleton<ISqlConnectionBuilder, DefaultSqlConnectionBuilder>());
        Assert.IsTrue(services.ContainsSingleton<SchemaInitializer>());
        Assert.IsTrue(services.ContainsSingleton<SchemaJobWorker>());
        Assert.IsTrue(services.ContainsSingleton<SchemaWriteGateEvaluator>());
        Assert.IsTrue(services.ContainsScoped<SchemaUpgradeRunner>());
        Assert.IsTrue(services.ContainsScoped<SchemaManagerDataStore>());
        Assert.IsTrue(services.ContainsSingleton<ScriptProvider<ExampleVersion>>());
        Assert.IsTrue(services.ContainsSingleton<SqlServerDataStoreConfiguration>());
    }

    [TestMethod]
    public void GivenEmptyServiceCollection_WhenAddingSqlServerConnection_ThenAddNewServices()
    {
        var services = new ServiceCollection();
        services.AddSqlServerConnection();

        Assert.IsTrue(services.ContainsScoped<SqlConnectionWrapperFactory>());
        Assert.IsTrue(services.ContainsScoped<SqlTransactionHandler>());
        Assert.IsTrue(services.ContainsScoped<ITransactionHandler, SqlTransactionHandler>());

        Assert.IsTrue(services.ContainsSingleton<ISqlConnectionBuilder, DefaultSqlConnectionBuilder>());
        Assert.IsTrue(services.ContainsScoped<IReadOnlySchemaManagerDataStore, SchemaManagerDataStore>());
    }

    [TestMethod]
    public void GivenEmptyServiceCollection_WhenAddingSqlServerManagement_ThenAddNewServices()
    {
        var services = new ServiceCollection();
        services.AddSqlServerManagement<ExampleVersion>();

        Assert.IsTrue(services.ContainsScoped<ISchemaDataStore, SqlServerSchemaDataStore>());

        Assert.IsTrue(services.ContainsSingleton<IBaseScriptProvider, BaseScriptProvider>());
        Assert.IsTrue(services.ContainsSingleton<IHostedService, SchemaInitializer>());
        Assert.IsTrue(services.ContainsScoped<ISchemaManagerDataStore>());
        Assert.IsTrue(services.ContainsSingleton<IScriptProvider, ScriptProvider<ExampleVersion>>());
        Assert.IsTrue(services.ContainsSingleton<SchemaJobWorker>());
        Assert.IsTrue(services.ContainsSingleton<SchemaWriteGateEvaluator>());
        Assert.IsTrue(services.ContainsScoped<SchemaUpgradeRunner>());
    }
}
