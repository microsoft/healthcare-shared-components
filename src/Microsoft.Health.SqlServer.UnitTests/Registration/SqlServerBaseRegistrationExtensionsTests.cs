// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Core.Features.Control;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;
using Microsoft.Health.SqlServer.Registration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Health.SqlServer.UnitTests.Registration;

[TestClass]
public class SqlServerBaseRegistrationExtensionsTests
{
    private const string FirstKey = "tenant1";
    private const string FirstDatabase = "Database1";
    private const string SecondKey = "tenant2";
    private const string SecondDatabase = "Database2";
    private const string Region = "eastus2";

    private enum ExampleVersion
    {
        V0,
        V1,
    }

    [TestMethod]
    public void GivenNullServices_WhenAddingSqlServerConnection_ThenThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SqlServerBaseRegistrationExtensions.AddSqlServerConnection(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => SqlServerBaseRegistrationExtensions.AddSqlServerConnection(null, o => { }));
    }

    [TestMethod]
    public void GivenNullConfigure_WhenAddingSqlServerConnection_ThenThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ServiceCollection().AddSqlServerConnection(null));
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
        Assert.IsTrue(services.ContainsSingleton<IProcessTerminator>());
    }

    [TestMethod]
    public void GivenConfigureDelegate_WhenAddingSqlServerConnection_ThenConfigureOptions()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(Configure((FirstKey, FirstDatabase)))
            .BuildServiceProvider();

        SqlServerDatabaseOptions database = provider.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value.Databases[FirstKey];
        Assert.Contains(FirstDatabase, database.ConnectionString, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenSingleDatabase_WhenResolvingSqlConnectionBuilder_ThenReturnDefaultBuilder()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(Configure((FirstKey, FirstDatabase)))
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope scope = provider.CreateScope();
        ISqlConnectionBuilder builder = scope.ServiceProvider.GetRequiredService<ISqlConnectionBuilder>();

        Assert.IsInstanceOfType<DefaultSqlConnectionBuilder>(builder);
        Assert.AreEqual(FirstDatabase, builder.DefaultDatabase);
        Assert.AreEqual(FirstDatabase, scope.ServiceProvider.GetRequiredService<SqlConnectionWrapperFactory>().DefaultDatabase);
    }

    [TestMethod]
    public void GivenConnectionString_WhenResolvingSqlConnectionBuilder_ThenReturnDefaultBuilderForThatDatabase()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(o =>
            {
                o.ConnectionString = $"server=(local);Initial Catalog={FirstDatabase}";
                o.Region = Region;
            })
            .BuildServiceProvider(validateScopes: true);

        ISqlConnectionBuilder builder = provider.GetRequiredService<ISqlConnectionBuilder>();

        Assert.IsInstanceOfType<DefaultSqlConnectionBuilder>(builder);
        Assert.AreEqual(FirstDatabase, builder.DefaultDatabase);
    }

    [TestMethod]
    public void GivenMultipleDatabases_WhenResolvingOptions_ThenPassValidation()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(Configure((FirstKey, FirstDatabase), (SecondKey, SecondDatabase)))
            .BuildServiceProvider();

        SqlServerDataStoreOptions options = provider.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value;

        Assert.HasCount(2, options.Databases);
        Assert.IsNull(options.ConnectionString);
    }

    [TestMethod]
    public void GivenMissingRegion_WhenResolvingOptions_ThenThrowOptionsValidationException()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(o => o.Databases = CreateDatabases((FirstKey, FirstDatabase)))
            .BuildServiceProvider();

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value);
        Assert.Contains(nameof(SqlServerDataStoreOptions.Region), ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenMissingDatabases_WhenResolvingOptions_ThenThrowOptionsValidationException()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(o => o.Region = Region)
            .BuildServiceProvider();

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value);
        Assert.Contains(nameof(SqlServerDataStoreOptions.Databases), ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenInvalidSchemaOptions_WhenResolvingOptions_ThenThrowOptionsValidationException()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(o =>
            {
                Configure((FirstKey, FirstDatabase))(o);
                o.SchemaOptions.JobPollingFrequency = TimeSpan.Zero;
            })
            .BuildServiceProvider();

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value);
        Assert.Contains($"{nameof(SqlServerDataStoreOptions)}.{nameof(SqlServerDataStoreOptions.SchemaOptions)}", ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(SqlServerSchemaOptions.JobPollingFrequency), ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenDatabaseWithoutConnectionString_WhenResolvingOptions_ThenThrowOptionsValidationException()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(o =>
            {
                Configure((FirstKey, FirstDatabase))(o);
                o.Databases = new Dictionary<string, SqlServerDatabaseOptions>(o.Databases) { [SecondKey] = new SqlServerDatabaseOptions() };
            })
            .BuildServiceProvider();

        OptionsValidationException ex = Assert.ThrowsExactly<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SqlServerDataStoreOptions>>().Value);
        Assert.Contains($"[{SecondKey}]", ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(SqlServerDatabaseOptions.ConnectionString), ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GivenNullServices_WhenAddingSqlServerManagement_ThenThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SqlServerBaseRegistrationExtensions.AddSqlServerManagement<ExampleVersion>(null));
    }

    [TestMethod]
    [DataRow(null, typeof(NoOpProcessTerminator))]
    [DataRow(2, typeof(ProcessTerminator))]
    public void GivenCompletionSchemaVersion_WhenResolvingProcessTerminator_ThenReturnExpectedType(int? completionSchemaVersion, Type expectedType)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IHostApplicationLifetime>());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services
            .AddSqlServerConnection(o =>
            {
                Configure((FirstKey, FirstDatabase))(o);
                o.SchemaOptions.CompletionSchemaVersion = completionSchemaVersion;
            })
            .AddSqlServerManagement<ExampleVersion>();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsInstanceOfType(provider.GetRequiredService<IProcessTerminator>(), expectedType);
    }

    [TestMethod]
    public void GivenNullServices_WhenAddingSqlServerDatabaseContext_ThenThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SqlServerBaseRegistrationExtensions.AddSqlServerDatabaseContext<TestDatabaseContext>(null));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GivenSqlServerConnection_WhenAddingSqlServerDatabaseContext_ThenReplaceConnectionBuilder(bool addContextFirst)
    {
        var services = new ServiceCollection();
        if (addContextFirst)
        {
            services.AddSqlServerDatabaseContext<TestDatabaseContext>();
            services.AddSqlServerConnection();
        }
        else
        {
            services.AddSqlServerConnection();
            services.AddSqlServerDatabaseContext<TestDatabaseContext>();
        }

        Assert.IsTrue(services.ContainsScoped<ISqlServerDatabaseContext, TestDatabaseContext>());
        Assert.IsTrue(services.ContainsScoped<ISqlConnectionBuilder, ScopedSqlConnectionBuilder>());
        Assert.ContainsSingle(services.Where(x => x.ServiceType == typeof(ISqlConnectionBuilder)));
    }

    [TestMethod]
    public void GivenExistingDatabaseContext_WhenAddingSqlServerDatabaseContext_ThenKeepExistingContext()
    {
        var services = new ServiceCollection();
        services.AddScoped<ISqlServerDatabaseContext, OtherDatabaseContext>();
        services.AddSqlServerDatabaseContext<TestDatabaseContext>();

        ServiceDescriptor descriptor = Assert.ContainsSingle(services.Where(x => x.ServiceType == typeof(ISqlServerDatabaseContext)));
        Assert.AreEqual(typeof(OtherDatabaseContext), descriptor.ImplementationType);
    }

    [TestMethod]
    public void GivenDatabaseContext_WhenResolvingSqlConnectionBuilderPerScope_ThenSelectDatabaseForEachScope()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(Configure((FirstKey, FirstDatabase), (SecondKey, SecondDatabase)))
            .AddSqlServerDatabaseContext<TestDatabaseContext>()
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope firstScope = provider.CreateScope();
        using IServiceScope secondScope = provider.CreateScope();
        SetDatabaseKey(firstScope, FirstKey);
        SetDatabaseKey(secondScope, SecondKey);

        ISqlConnectionBuilder firstBuilder = firstScope.ServiceProvider.GetRequiredService<ISqlConnectionBuilder>();
        ISqlConnectionBuilder secondBuilder = secondScope.ServiceProvider.GetRequiredService<ISqlConnectionBuilder>();

        Assert.IsInstanceOfType<ScopedSqlConnectionBuilder>(firstBuilder);
        Assert.IsInstanceOfType<ScopedSqlConnectionBuilder>(secondBuilder);
        Assert.AreSame(firstBuilder, firstScope.ServiceProvider.GetRequiredService<ISqlConnectionBuilder>());
        Assert.AreNotSame(firstBuilder, secondBuilder);

        Assert.AreEqual(FirstDatabase, firstBuilder.DefaultDatabase);
        Assert.AreEqual(SecondDatabase, secondBuilder.DefaultDatabase);
        Assert.AreEqual(FirstDatabase, firstScope.ServiceProvider.GetRequiredService<SqlConnectionWrapperFactory>().DefaultDatabase);
        Assert.AreEqual(SecondDatabase, secondScope.ServiceProvider.GetRequiredService<SqlConnectionWrapperFactory>().DefaultDatabase);
    }

    [TestMethod]
    public void GivenUnknownDatabaseKey_WhenResolvingSqlConnectionBuilder_ThenThrowArgumentException()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection(Configure((FirstKey, FirstDatabase)))
            .AddSqlServerDatabaseContext<TestDatabaseContext>()
            .BuildServiceProvider(validateScopes: true);

        using IServiceScope scope = provider.CreateScope();
        SetDatabaseKey(scope, "unknown");

        Assert.ThrowsExactly<ArgumentException>(() => scope.ServiceProvider.GetRequiredService<ISqlConnectionBuilder>());
    }

    [TestMethod]
    public void GivenNoDatabaseContext_WhenCheckingMultipleDatabaseSupport_ThenDoNotThrow()
    {
        using ServiceProvider provider = new ServiceCollection().AddSqlServerConnection().BuildServiceProvider();

        provider.ThrowIfMultipleDatabasesSupported();
    }

    [TestMethod]
    public void GivenDatabaseContext_WhenCheckingMultipleDatabaseSupport_ThenThrowNotSupportedException()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSqlServerConnection()
            .AddSqlServerDatabaseContext<TestDatabaseContext>()
            .BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();

        Assert.ThrowsExactly<NotSupportedException>(() => scope.ServiceProvider.ThrowIfMultipleDatabasesSupported());
    }

    [TestMethod]
    public void GivenNullServiceProvider_WhenCheckingMultipleDatabaseSupport_ThenThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SqlServerBaseRegistrationExtensions.ThrowIfMultipleDatabasesSupported(null));
    }

    private static void SetDatabaseKey(IServiceScope scope, string key)
        => ((TestDatabaseContext)scope.ServiceProvider.GetRequiredService<ISqlServerDatabaseContext>()).Key = key;

    private static Action<SqlServerDataStoreOptions> Configure(params (string Key, string Database)[] databases)
        => o =>
        {
            o.Databases = CreateDatabases(databases);
            o.Region = Region;
        };

    private static Dictionary<string, SqlServerDatabaseOptions> CreateDatabases(params (string Key, string Database)[] databases)
        => databases.ToDictionary(
            x => x.Key,
            x => new SqlServerDatabaseOptions { ConnectionString = $"server=(local);Initial Catalog={x.Database}" });

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This type is activated via a service provider.")]
    private sealed class TestDatabaseContext : ISqlServerDatabaseContext
    {
        public string Key { get; set; }
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "This type is activated via a service provider.")]
    private sealed class OtherDatabaseContext : ISqlServerDatabaseContext
    {
        public string Key => "other";
    }
}
