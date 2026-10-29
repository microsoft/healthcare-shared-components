// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using EnsureThat;
using Microsoft.Data.SqlClient;
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

        services
            .AddOptions<SqlServerDataStoreOptions>()
            .ValidateDataAnnotations();

        services.AddSqlRetryLogicProvider();
        services.TryAddSingleton<ISqlConnectionBuilder, DefaultSqlConnectionBuilder>();

        // Services to facilitate SQL connections
        // TODO: Does SqlTransactionHandler need to be registered directly? Should usage change to ITransactionHandler?
        Func<IServiceProvider, SqlTransactionHandler> handlerFactory = p => p.GetRequiredService<SqlTransactionHandler>();

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
    /// <param name="configure">A delegate for configuring the <see cref="SqlServerDataStoreOptions"/>.</param>
    /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, Action<SqlServerDataStoreOptions> configure)
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
        services.TryAddSingleton<ISchemaWriteGate, DefaultSchemaWriteGate>();
        services.TryAddSingleton<ISchemaMetrics, SchemaMetrics>();
        services.TryAddSingleton<SchemaWriteGateEvaluator>();
        services.TryAddSingleton<IScriptProvider, ScriptProvider<TVersion>>();
        services.TryAddSingleton<IBaseScriptProvider, BaseScriptProvider>();
        services.TryAddScoped<SchemaUpgradeRunner>();
        services.AddHostedService<SchemaInitializer>();

        // Resolve IProcessTerminator based on the configuration
        services.TryAddSingleton<IProcessTerminator>(
            p =>
            {
                SqlServerDataStoreOptions config = p.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value;
                return config.SchemaOptions?.CompletionSchemaVersion is not null
                    ? new ProcessTerminator(p.GetRequiredService<IHostApplicationLifetime>())
                    : new NoOpProcessTerminator(p.GetRequiredService<ILogger<NoOpProcessTerminator>>());
            });

        // Re-use the existing SchemaManagerDataStore
        services.TryAddScoped<ISchemaManagerDataStore>(
            p =>
            {
                var schemaManagerDataStore = p.GetService<IReadOnlySchemaManagerDataStore>() as SchemaManagerDataStore;
                return schemaManagerDataStore ?? new SchemaManagerDataStore(p.GetRequiredService<SqlConnectionWrapperFactory>(), p.GetRequiredService<IOptions<SqlServerDataStoreOptions>>(), p.GetRequiredService<ILogger<SchemaManagerDataStore>>());
            });

        return services;
    }

    /// <summary>
    /// Adds support for connecting to one of multiple SQL databases, selected per scope by an
    /// <see cref="ISqlServerDatabaseContext"/>, to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TContext">The type of <see cref="ISqlServerDatabaseContext"/> that identifies the database for the current scope.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
    /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddSqlServerDatabaseContext<TContext>(this IServiceCollection services)
        where TContext : class, ISqlServerDatabaseContext
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.TryAddScoped<ISqlServerDatabaseContext, TContext>();
        services.Replace(ServiceDescriptor.Scoped<ISqlConnectionBuilder, ScopedSqlConnectionBuilder>());

        return services;
    }

    internal static void ThrowIfMultipleDatabasesSupported(this IServiceProvider serviceProvider)
    {
        EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));

        if (serviceProvider.GetService<ISqlServerDatabaseContext>() is not null)
            throw new NotSupportedException("Multiple databases are not supported.");
    }

    /// <summary>
    /// Adds an <see cref="SqlRetryLogicBaseProvider"/> to be used by SqlConnection and SqlCommand
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to be updated.</param>
    /// <returns>The <paramref name="services"/> for additional method invocations.</returns>
    /// <exception cref="NotImplementedException">When the retry mode is unkown</exception>
    private static IServiceCollection AddSqlRetryLogicProvider(this IServiceCollection services)
    {
        services.TryAddSingleton(p =>
        {
            SqlServerDataStoreOptions config = p.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value;

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
