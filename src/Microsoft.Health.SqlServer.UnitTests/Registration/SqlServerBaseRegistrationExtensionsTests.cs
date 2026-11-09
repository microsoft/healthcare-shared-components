// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
        Assert.IsTrue(services.ContainsSingleton<IPostConfigureOptions<SqlSchemaVersionOptions>, ConfigureSchemaInformation>());
    }

    [TestMethod]
    public void GivenSchemaInformation_WhenResolvingSqlSchemaVersionOptions_ThenVersionsArePopulated()
    {
        var services = new ServiceCollection();
        services.AddSqlServerManagement<ExampleVersion>();
        services.AddSingleton(new SchemaInformation(2, 7));

        using ServiceProvider provider = services.BuildServiceProvider();
        SqlSchemaVersionOptions options = provider.GetRequiredService<IOptions<SqlSchemaVersionOptions>>().Value;

        Assert.AreEqual(2, options.MinimumVersion);
        Assert.AreEqual(7, options.MaximumVersion);
    }

    [TestMethod]
    public void GivenNoSchemaInformation_WhenResolvingSqlSchemaVersionOptions_ThenDefaultsAreUsed()
    {
        var services = new ServiceCollection();
        services.AddSqlServerManagement<ExampleVersion>();

        using ServiceProvider provider = services.BuildServiceProvider();
        SqlSchemaVersionOptions options = provider.GetRequiredService<IOptions<SqlSchemaVersionOptions>>().Value;

        Assert.AreEqual(1, options.MinimumVersion);
        Assert.AreEqual(1, options.MaximumVersion);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 0)]
    public void GivenNonPositiveVersion_WhenResolvingSqlSchemaVersionOptions_ThenValidationFails(int minimumVersion, int maximumVersion)
    {
        var services = new ServiceCollection();
        services.AddSqlServerManagement<ExampleVersion>();
        services.Configure<SqlSchemaVersionOptions>(o =>
        {
            o.MinimumVersion = minimumVersion;
            o.MaximumVersion = maximumVersion;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.ThrowsExactly<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SqlSchemaVersionOptions>>().Value);
    }

    [TestMethod]
    public void GivenSqlServerManagementAddedTwice_WhenRegistering_ThenPostConfigureIsRegisteredOnce()
    {
        var services = new ServiceCollection();
        services.AddSqlServerManagement<ExampleVersion>();
        services.AddSqlServerManagement<ExampleVersion>();

        Assert.ContainsSingle(d => d.ServiceType == typeof(IPostConfigureOptions<SqlSchemaVersionOptions>), services);
    }
}
