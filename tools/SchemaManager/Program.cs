// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Storage;

namespace SchemaManager;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        ServiceProvider serviceProvider = BuildServiceProvider(args);
        Parser parser = BuildParser(serviceProvider);

        return await parser.InvokeAsync(args).ConfigureAwait(false);
    }

    private static Parser BuildParser(ServiceProvider serviceProvider)
    {
        var commandLineBuilder = new CommandLineBuilder();

        foreach (Command command in serviceProvider.GetServices<Command>())
        {
            commandLineBuilder.AddCommand(command);
        }

        return commandLineBuilder.UseDefaults().Build();
    }

    private static ServiceProvider BuildServiceProvider(string[] args)
    {
        var services = new ServiceCollection();

        services.AddCliCommands();

        services.AddOptions();

        var builder = new ConfigurationBuilder();

        builder.AddCommandLine(args);

        SetCommandLineOptions(services, builder);

        services.AddHttpClient<ISchemaClient, SchemaClient>((sp, client) =>
        {
            CommandLineOptions args = sp.GetRequiredService<IOptions<CommandLineOptions>>().Value;
            client.BaseAddress = args.Server;
        });

        // TODO: this won't work in OSS if the AuthenticationType is set to ManagedIdentity
        services.AddSingleton<ISqlConnectionBuilder, DefaultSqlConnectionBuilder>();

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

        services.AddOptions<SqlServerDataStoreConfiguration>().Configure<IOptions<CommandLineOptions>>((s, c) =>
        {
            s.ConnectionString = c.Value.ConnectionString;
        });

        services.AddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();
        services.AddScoped<IBaseSchemaRunner, BaseSchemaRunner>();
        services.AddScoped<SqlConnectionWrapperFactory>();
        services.AddScoped<SqlTransactionHandler>();
        services.AddScoped<ISchemaManagerDataStore, SchemaManagerDataStore>();
        services.AddSingleton<ISchemaManager, SqlSchemaManager>();
        services.AddLogging(configure => configure.AddConsole());
        return services.BuildServiceProvider();
    }

    private static void SetCommandLineOptions(ServiceCollection services, ConfigurationBuilder builder)
    {
        IConfigurationRoot config = builder.Build();

        services.AddOptions<CommandLineOptions>().Configure(x =>
        {
            x.ConnectionString = config["connection-string"];

            if (!string.IsNullOrWhiteSpace(config["server"]))
            {
                x.Server = new Uri(config["server"]);
            }
        });
    }
}
