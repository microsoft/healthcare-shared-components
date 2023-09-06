// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Registration;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests.Registration;

public class SqlServerBaseRegistrationExtensionsTests
{
    private enum ExampleVersion
    {
        V0,
        V1,
    }

    [Fact]
    [Obsolete("To be removed when AddSqlServerBase is deleted.")]
    public void GivenEmptyServiceCollection_WhenAddingSqlServerBase_ThenAddNewServices()
    {
        var services = new ServiceCollection();
        services.AddSqlServerBase<ExampleVersion>(null);

        Assert.True(services.ContainsScoped<ISchemaDataStore, SqlServerSchemaDataStore>());
        Assert.True(services.ContainsScoped<ITransactionHandler, SqlTransactionHandler>());
        Assert.True(services.ContainsScoped<SqlConnectionWrapperFactory>());
        Assert.True(services.ContainsScoped<SqlServerSchemaDataStore>());
        Assert.True(services.ContainsScoped<SqlTransactionHandler>());

        Assert.True(services.ContainsSingleton<DefaultAzureCredential>());
        Assert.True(services.ContainsSingleton<BaseScriptProvider>());
        Assert.True(services.ContainsSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>());
        Assert.True(services.ContainsSingleton<IAccessTokenHandler, WorkloadIdentityAccessTokenHandler>());
        Assert.True(services.ContainsSingleton<IBaseScriptProvider, BaseScriptProvider>());
        Assert.True(services.ContainsSingleton<IHostedService, SchemaInitializer>());
        Assert.True(services.ContainsScoped<ISchemaManagerDataStore>());
        Assert.True(services.ContainsSingleton<IScriptProvider, ScriptProvider<ExampleVersion>>());
        Assert.True(services.ContainsSingleton<ISqlConnectionBuilder>());
        Assert.True(services.ContainsSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>());
        Assert.True(services.ContainsSingleton<SchemaInitializer>());
        Assert.True(services.ContainsSingleton<SchemaJobWorker>());
        Assert.True(services.ContainsScoped<SchemaUpgradeRunner>());
        Assert.True(services.ContainsScoped<SchemaManagerDataStore>());
        Assert.True(services.ContainsSingleton<ScriptProvider<ExampleVersion>>());
        Assert.True(services.ContainsSingleton<SqlServerDataStoreConfiguration>());
    }

    [Fact]
    public void GivenEmptyServiceCollection_WhenAddingSqlServerConnection_ThenAddNewServices()
    {
        var services = new ServiceCollection();
        services.AddSqlServerConnection();

        Assert.True(services.ContainsScoped<SqlConnectionWrapperFactory>());
        Assert.True(services.ContainsScoped<SqlTransactionHandler>());
        Assert.True(services.ContainsScoped<ITransactionHandler, SqlTransactionHandler>());

        Assert.True(services.ContainsSingleton<DefaultAzureCredential>());
        Assert.True(services.ContainsSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>());
        Assert.True(services.ContainsSingleton<IAccessTokenHandler, WorkloadIdentityAccessTokenHandler>());
        Assert.True(services.ContainsSingleton<ISqlConnectionBuilder>());
        Assert.True(services.ContainsSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>());
        Assert.True(services.ContainsScoped<IReadOnlySchemaManagerDataStore, SchemaManagerDataStore>());
    }

    [Fact]
    public void GivenEmptyServiceCollection_WhenAddingSqlServerManagement_ThenAddNewServices()
    {
        var services = new ServiceCollection();
        services.AddSqlServerManagement<ExampleVersion>();

        Assert.True(services.ContainsScoped<ISchemaDataStore, SqlServerSchemaDataStore>());

        Assert.True(services.ContainsSingleton<IBaseScriptProvider, BaseScriptProvider>());
        Assert.True(services.ContainsSingleton<IHostedService, SchemaInitializer>());
        Assert.True(services.ContainsScoped<ISchemaManagerDataStore>());
        Assert.True(services.ContainsSingleton<IScriptProvider, ScriptProvider<ExampleVersion>>());
        Assert.True(services.ContainsSingleton<SchemaJobWorker>());
        Assert.True(services.ContainsScoped<SchemaUpgradeRunner>());
    }
}
