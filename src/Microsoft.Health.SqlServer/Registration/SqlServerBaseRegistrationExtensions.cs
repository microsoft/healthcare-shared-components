// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Registration
{
    public static class SqlServerBaseRegistrationExtensions
    {
        public static IServiceCollection AddSqlServerBase<TSchemaVersionEnum>(
            this IServiceCollection services,
            IConfiguration configurationRoot,
            Action<SqlServerDataStoreConfiguration> configureAction = null,
            bool useAlternativeHostedService = false)
            where TSchemaVersionEnum : Enum
        {
            var config = new SqlServerDataStoreConfiguration();
            configurationRoot?.GetSection("SqlServer").Bind(config);

            services.Add(provider =>
                {
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

            if (useAlternativeHostedService)
            {
                services.Add<SchemaInitializer>()
                    .Singleton()
                    .AsSelf()
                    .AsService<IHostedServiceWrap>();
            }
            else
            {
                services.Add<SchemaInitializer>()
                    .Singleton()
                    .AsSelf()
                    .AsService<IHostedService>();
            }

            services.Add<ScriptProvider<TSchemaVersionEnum>>()
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<BaseScriptProvider>()
               .Singleton()
               .AsSelf()
               .AsImplementedInterfaces();

            services.Add<SqlTransactionHandler>()
                .Scoped()
                .AsSelf()
                .AsImplementedInterfaces();

            services.Add<SchemaManagerDataStore>()
                .Singleton()
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

            services.AddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();

            switch (config.AuthenticationType)
            {
                case SqlServerAuthenticationType.ManagedIdentity:
                    services.AddSingleton<ISqlConnectionFactory, ManagedIdentitySqlConnectionFactory>();
                    services.AddSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>();
                    services.AddSingleton<AzureServiceTokenProvider>();
                    break;
                case SqlServerAuthenticationType.ConnectionString:
                default:
                    services.AddSingleton<ISqlConnectionFactory, DefaultSqlConnectionFactory>();
                    break;
            }

            return services;
        }
    }
}
