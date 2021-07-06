// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Registration
{
    public static class SqlServerBaseRegistrationExtensions
    {
        [Obsolete("Please use " + nameof(AddSqlServerConnection) + " and " + nameof(AddSqlServerVersioningService) + " instead.")]
        public static IServiceCollection AddSqlServerBase<TSchemaVersionEnum>(
            this IServiceCollection services,
            IConfiguration configurationRoot,
            Action<SqlServerDataStoreConfiguration> configureAction = null)
            where TSchemaVersionEnum : Enum
        {
            services.AddSqlServerConnection(
                config =>
                {
                    configurationRoot?.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(config);
                    configureAction?.Invoke(config);
                });

            services.AddSqlServerVersioningService<TSchemaVersionEnum>();

            // Add more services for backward compatibility
            services.TryAddScoped(p => p.GetRequiredService<ISchemaDataStore>() as SqlServerSchemaDataStore);
            services.TryAddSingleton(provider => provider.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>().Value);
            services.TryAddSingleton(p => p.GetServices<IHostedService>().First(x => x is SchemaInitializer) as SchemaInitializer);
            services.TryAddSingleton(p => p.GetRequiredService<IScriptProvider>() as ScriptProvider<TSchemaVersionEnum>);
            services.TryAddSingleton(p => p.GetRequiredService<IBaseScriptProvider>() as BaseScriptProvider);
            services.TryAddSingleton(p => p.GetRequiredService<ISchemaManagerDataStore>() as SchemaManagerDataStore);
            services.TryAddSingleton(p => p.GetRequiredService<IPollyRetryLoggerFactory>() as PollyRetryLoggerFactory);
            services.TryAddSingleton(p => p.GetRequiredService<SqlCommandWrapperFactory>() as RetrySqlCommandWrapperFactory);
            services.TryAddSingleton(p => p.GetRequiredService<ISqlServerTransientFaultRetryPolicyFactory>() as SqlServerTransientFaultRetryPolicyFactory);

            return services;
        }

        /// <summary>
        ///  Adds a collection of services for connecting to SQL Server to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddSqlServerConnection(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.AddOptions();
            services.TryAddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();
            services.TryAddSingleton<ISqlConnectionFactory>(
                p =>
                {
                    SqlServerDataStoreConfiguration config = p.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>().Value;
                    ISqlConnectionStringProvider sqlConnectionStringProvider = p.GetRequiredService<ISqlConnectionStringProvider>();
                    return config.AuthenticationType == SqlServerAuthenticationType.ManagedIdentity
                        ? new ManagedIdentitySqlConnectionFactory(sqlConnectionStringProvider, p.GetRequiredService<IAccessTokenHandler>())
                        : new DefaultSqlConnectionFactory(sqlConnectionStringProvider);
                });

            // The following are only used in case of managed identity
            services.AddSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>();
            services.AddSingleton<AzureServiceTokenProvider>();

            // Services to facilitate SQL connections
            // TODO: Does SqlTransactionHandler need to be registered directly? Should usage change to ITransactionHandler?
            Func<IServiceProvider, SqlTransactionHandler> handlerFactory = p => p.GetRequiredService<SqlTransactionHandler>();

            services.TryAddScoped<SqlConnectionWrapperFactory>();
            services.TryAddScoped<SqlTransactionHandler>();
            services.TryAddScoped<ITransactionHandler>(handlerFactory);
            services.TryAddSingleton<IPollyRetryLoggerFactory, PollyRetryLoggerFactory>();
            services.TryAddSingleton<ISqlServerTransientFaultRetryPolicyFactory, SqlServerTransientFaultRetryPolicyFactory>();
            services.TryAddSingleton<SqlCommandWrapperFactory, RetrySqlCommandWrapperFactory>();

            return services;
        }

        /// <summary>
        ///  Adds a collection of services for connecting to SQL Server to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <param name="configure">A delegate for configuring the <see cref="SqlServerDataStoreConfiguration"/>.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddSqlServerConnection(
            this IServiceCollection services,
            Action<SqlServerDataStoreConfiguration> configure)
        {
            EnsureArg.IsNotNull(configure, nameof(configure));

            return services
                .AddSqlServerConnection()
                .Configure(configure);
        }

        /// <summary>
        ///  Adds a hosted service for updating the application's SQL database to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TVersion">The type of the version enumeration.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
        /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IServiceCollection AddSqlServerVersioningService<TVersion>(this IServiceCollection services)
            where TVersion : Enum
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.TryAddScoped<ISchemaDataStore, SqlServerSchemaDataStore>();
            services.TryAddSingleton<SchemaJobWorker>();
            services.TryAddSingleton<IScriptProvider, ScriptProvider<TVersion>>();
            services.TryAddSingleton<IBaseScriptProvider, BaseScriptProvider>();
            services.TryAddSingleton<ISchemaManagerDataStore, SchemaManagerDataStore>();
            services.TryAddSingleton<SchemaUpgradeRunner>();
            services.AddHostedService<SchemaInitializer>();

            return services;
        }
    }
}
