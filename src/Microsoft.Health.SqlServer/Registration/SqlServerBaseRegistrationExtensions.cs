﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Registration
{
    public static class SqlServerBaseRegistrationExtensions
    {
        public static IServiceCollection AddSqlServerBase<TSchemaVersionEnum>(
            this IServiceCollection services,
            Action<SqlServerDataStoreConfiguration> configureAction = null)
            where TSchemaVersionEnum : Enum
        {
            services.Add(provider =>
                {
                    var config = new SqlServerDataStoreConfiguration();
                    provider.GetService<IConfiguration>().GetSection("SqlServer").Bind(config);
                    configureAction?.Invoke(config);

                    return config;
                })
                .Singleton()
                .AsSelf();

            services.Add<SchemaUpgradeRunner>()
                .Singleton()
                .AsSelf();

            services.Add<SqlServerSchemaDataStore>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SchemaJobWorker>()
                .Singleton()
                .AsSelf();

            services.Add<SchemaInitializer>()
                .Singleton()
                .AsSelf()
                .AsService<IStartable>();

            services.Add<ScriptProvider<TSchemaVersionEnum>>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlTransactionHandler>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<PollyRetryLoggerFactory>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SqlServerTransientFaultRetryPolicyFactory>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<RetrySqlCommandWrapperFactory>()
                .Singleton()
                .AsSelf()
                .AsService<SqlCommandWrapperFactory>();

            services.Add<SqlConnectionWrapperFactory>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            return services;
        }
    }
}
