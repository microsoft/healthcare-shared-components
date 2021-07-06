﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.Services.AppAuthentication;
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

namespace Microsoft.Health.SqlServer.UnitTests.Registration
{
    public class SqlServerBaseRegistrationExtensionsTests
    {
        private enum ExampleVersion
        {
            V0,
            V1,
        }

        [Fact]
        [Obsolete]
        public void GivenEmptyServiceCollection_WhenAddingSqlServerBase_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddSqlServerBase<ExampleVersion>(null);

            Assert.True(services.ContainsScoped<ISchemaDataStore, SqlServerSchemaDataStore>());
            Assert.True(services.ContainsScoped<ITransactionHandler, SqlTransactionHandler>());
            Assert.True(services.ContainsScoped<SqlConnectionWrapperFactory>());
            Assert.True(services.ContainsScoped<SqlServerSchemaDataStore>());
            Assert.True(services.ContainsScoped<SqlTransactionHandler>());

            Assert.True(services.ContainsSingleton<AzureServiceTokenProvider>());
            Assert.True(services.ContainsSingleton<BaseScriptProvider>());
            Assert.True(services.ContainsSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>());
            Assert.True(services.ContainsSingleton<IBaseScriptProvider, BaseScriptProvider>());
            Assert.True(services.ContainsSingleton<IHostedService, SchemaInitializer>());
            Assert.True(services.ContainsSingleton<IPollyRetryLoggerFactory, PollyRetryLoggerFactory>());
            Assert.True(services.ContainsSingleton<ISchemaManagerDataStore, SchemaManagerDataStore>());
            Assert.True(services.ContainsSingleton<IScriptProvider, ScriptProvider<ExampleVersion>>());
            Assert.True(services.ContainsSingleton<ISqlConnectionFactory>());
            Assert.True(services.ContainsSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>());
            Assert.True(services.ContainsSingleton<ISqlServerTransientFaultRetryPolicyFactory, SqlServerTransientFaultRetryPolicyFactory>());
            Assert.True(services.ContainsSingleton<PollyRetryLoggerFactory>());
            Assert.True(services.ContainsSingleton<RetrySqlCommandWrapperFactory>());
            Assert.True(services.ContainsSingleton<SchemaInitializer>());
            Assert.True(services.ContainsSingleton<SchemaJobWorker>());
            Assert.True(services.ContainsSingleton<SchemaUpgradeRunner>());
            Assert.True(services.ContainsSingleton<SchemaManagerDataStore>());
            Assert.True(services.ContainsSingleton<ScriptProvider<ExampleVersion>>());
            Assert.True(services.ContainsSingleton<SqlCommandWrapperFactory, RetrySqlCommandWrapperFactory>());
            Assert.True(services.ContainsSingleton<SqlServerDataStoreConfiguration>());
            Assert.True(services.ContainsSingleton<SqlServerTransientFaultRetryPolicyFactory>());
        }

        [Fact]
        public void GivenEmptyServiceCollection_WhenAddingSqlServerConnection_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddSqlServerConnection();

            Assert.True(services.ContainsScoped<SqlConnectionWrapperFactory>());
            Assert.True(services.ContainsScoped<SqlTransactionHandler>());
            Assert.True(services.ContainsScoped<ITransactionHandler, SqlTransactionHandler>());

            Assert.True(services.ContainsSingleton<AzureServiceTokenProvider>());
            Assert.True(services.ContainsSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>());
            Assert.True(services.ContainsSingleton<IPollyRetryLoggerFactory, PollyRetryLoggerFactory>());
            Assert.True(services.ContainsSingleton<ISqlConnectionFactory>());
            Assert.True(services.ContainsSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>());
            Assert.True(services.ContainsSingleton<ISqlServerTransientFaultRetryPolicyFactory, SqlServerTransientFaultRetryPolicyFactory>());
            Assert.True(services.ContainsSingleton<SqlCommandWrapperFactory, RetrySqlCommandWrapperFactory>());
        }

        [Fact]
        public void GivenEmptyServiceCollection_WhenAddingSqlServerVersioningService_ThenAddNewServices()
        {
            var services = new ServiceCollection();
            services.AddSqlServerVersioningService<ExampleVersion>();

            Assert.True(services.ContainsScoped<ISchemaDataStore, SqlServerSchemaDataStore>());

            Assert.True(services.ContainsSingleton<IBaseScriptProvider, BaseScriptProvider>());
            Assert.True(services.ContainsSingleton<IHostedService, SchemaInitializer>());
            Assert.True(services.ContainsSingleton<ISchemaManagerDataStore, SchemaManagerDataStore>());
            Assert.True(services.ContainsSingleton<IScriptProvider, ScriptProvider<ExampleVersion>>());
            Assert.True(services.ContainsSingleton<SchemaJobWorker>());
            Assert.True(services.ContainsSingleton<SchemaUpgradeRunner>());
        }
    }
}
