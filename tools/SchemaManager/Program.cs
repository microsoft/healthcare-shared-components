// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Manager;

namespace SchemaManager
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            ServiceProvider serviceProvider = BuildServiceProvider();
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

        private static ServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddCliCommands();

            // Add SqlServer services
            services.AddSingleton<SqlServerDataStoreConfiguration>();
            services.AddSingleton<ISqlConnectionFactory, DefaultSqlConnectionFactory>();
            services.AddSingleton<BaseSchemaRunner>();
            services.AddSingleton<ISchemaManagerDataStore, SchemaManagerDataStore>();
            services.AddSingleton<ISchemaClient, SchemaClient>();

            return services.BuildServiceProvider();
        }
    }
}
