// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Core.Features.Control;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Registration;

public static class SqlServerBaseRegistrationExtensions
{
    [Obsolete("Please use " + nameof(AddSqlServerConnection) + " and " + nameof(AddSqlServerManagement) + " instead.")]
    public static IServiceCollection AddSqlServerBase<TSchemaVersionEnum>(
        this IServiceCollection services,
        IConfiguration configurationRoot,
        Action<SqlServerDataStoreConfiguration> configureAction = null)
        where TSchemaVersionEnum : Enum
    {
        services
            .AddSqlServerConnection(
                config =>
                {
                    configurationRoot?.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(config);
                    configureAction?.Invoke(config);
                })
            .AddSqlServerManagement<TSchemaVersionEnum>();

        // Add more services for backward compatibility
        services.TryAddScoped(p => p.GetRequiredService<ISchemaDataStore>() as SqlServerSchemaDataStore);
        services.TryAddSingleton(provider => provider.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>().Value);
        services.TryAddSingleton(p => p.GetServices<IHostedService>().First(x => x is SchemaInitializer) as SchemaInitializer);
        services.TryAddSingleton(p => p.GetRequiredService<IScriptProvider>() as ScriptProvider<TSchemaVersionEnum>);
        services.TryAddSingleton(p => p.GetRequiredService<IBaseScriptProvider>() as BaseScriptProvider);
        services.TryAddScoped(p => p.GetRequiredService<ISchemaManagerDataStore>() as SchemaManagerDataStore);
        services.AddSqlRetryLogicProvider();

        return services;
    }

    /// <summary>
    /// Adds a collection of services for connecting to SQL Server to the specified <see cref="IServiceCollection"/>.
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
        services.AddSqlRetryLogicProvider();
        services.TryAddSingleton<ISqlConnectionBuilder>(
             p =>
             {
                 var sqlServerDataStoreConfigOption = p.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>();
                 SqlServerDataStoreConfiguration config = sqlServerDataStoreConfigOption.Value;
                 ISqlConnectionStringProvider sqlConnectionStringProvider = p.GetRequiredService<ISqlConnectionStringProvider>();
                 SqlRetryLogicBaseProvider sqlRetryLogic = p.GetRequiredService<SqlRetryLogicBaseProvider>();
                 return config.AuthenticationType == SqlServerAuthenticationType.ManagedIdentity
                     ? new ManagedIdentitySqlConnectionBuilder(sqlConnectionStringProvider, p.GetRequiredService<IAccessTokenHandler>(), sqlRetryLogic)
                     : new DefaultSqlConnectionBuilder(sqlConnectionStringProvider, sqlRetryLogic);
             });

        // The following are only used in case of managed identity
        services.AddSingleton<IAccessTokenHandler, ManagedIdentityAccessTokenHandler>();
        services.AddSingleton<AzureServiceTokenProvider>(p =>
        {
            SqlServerDataStoreConfiguration config = p.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>().Value;

            string tokenProviderConnectionString = null;

            if (!string.IsNullOrEmpty(config.ManagedIdentityClientId))
            {
                tokenProviderConnectionString = $"RunAs=App;AppId={config.ManagedIdentityClientId}";
            }

            return new AzureServiceTokenProvider(connectionString: tokenProviderConnectionString);
        });

        // Services to facilitate SQL connections
        // TODO: Does SqlTransactionHandler need to be registered directly? Should usage change to ITransactionHandler?
        Func<IServiceProvider, SqlTransactionHandler> handlerFactory = p => p.GetRequiredService<SqlTransactionHandler>();

        services.AddLogging();
        services.TryAddScoped<SqlConnectionWrapperFactory>();
        services.TryAddScoped<SqlTransactionHandler>();
        services.TryAddScoped<ITransactionHandler>(handlerFactory);
        services.TryAddScoped<IReadOnlySchemaManagerDataStore, SchemaManagerDataStore>();

        return services;
    }

    /// <summary>
    /// Adds a collection of services for connecting to SQL Server to the specified <see cref="IServiceCollection"/>.
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
    /// Adds an <see cref="IHostedService"/> to the specified <see cref="IServiceCollection"/>
    /// for managing the configured SQL application database.
    /// </summary>
    /// <typeparam name="TVersion">The type of the version enumeration.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
    /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddSqlServerManagement<TVersion>(this IServiceCollection services)
        where TVersion : Enum
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.TryAddScoped<ISchemaDataStore, SqlServerSchemaDataStore>();
        services.TryAddSingleton<SchemaJobWorker>();
        services.TryAddSingleton<IScriptProvider, ScriptProvider<TVersion>>();
        services.TryAddSingleton<IBaseScriptProvider, BaseScriptProvider>();
        services.TryAddScoped<SchemaUpgradeRunner>();
        services.AddHostedService<SchemaInitializer>();

        // Resolve IProcessTerminator based on the configuration
        services.TryAddSingleton<IProcessTerminator>(
            p =>
            {
                SqlServerDataStoreConfiguration config = p.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>().Value;
                return config.TerminateWhenSchemaVersionUpdatedTo.HasValue
                    ? new ProcessTerminator(p.GetRequiredService<IHostApplicationLifetime>())
                    : new NoOpProcessTerminator(p.GetRequiredService<ILogger<NoOpProcessTerminator>>());
            });

        // Re-use the existing SchemaManagerDataStore
        services.TryAddScoped<ISchemaManagerDataStore>(
            p =>
            {
                var schemaManagerDataStore = p.GetService<IReadOnlySchemaManagerDataStore>() as SchemaManagerDataStore;
                return schemaManagerDataStore != null
                    ? schemaManagerDataStore
                    : new SchemaManagerDataStore(p.GetRequiredService<SqlConnectionWrapperFactory>(), p.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>(), p.GetRequiredService<ILogger<SchemaManagerDataStore>>());
            });

        return services;
    }

    /// <summary>
    /// Adds an <see cref="SqlRetryLogicBaseProvider"/> to be used by SqlConnection and SqlCommand
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
    /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
    /// <exception cref="NotImplementedException">When the retry mode is unkown</exception>
    private static IServiceCollection AddSqlRetryLogicProvider(this IServiceCollection services)
    {
        services.TryAddSingleton<SqlRetryLogicBaseProvider>(p =>
        {
            SqlServerDataStoreConfiguration config = p.GetRequiredService<IOptions<SqlServerDataStoreConfiguration>>().Value;

            return config.Retry.Mode switch
            {
                SqlRetryMode.None => SqlConfigurableRetryFactory.CreateNoneRetryProvider(),
                SqlRetryMode.Fixed => SqlConfigurableRetryFactory.CreateFixedRetryProvider(config.Retry.Settings),
                SqlRetryMode.Incremental => SqlConfigurableRetryFactory.CreateIncrementalRetryProvider(config.Retry.Settings),
                SqlRetryMode.Exponential => SqlConfigurableRetryFactory.CreateExponentialRetryProvider(config.Retry.Settings),
                _ => throw new NotImplementedException(),
            };
        });
        return services;
    }
}
