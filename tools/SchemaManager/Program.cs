// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using SchemaManager.Core;

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
            services.AddOptions();
            services.AddHttpClient();

            // TODO: this won't work in OSS if the AuthenticationType is set to ManagedIdentity
            services.AddSingleton<ISqlConnection, DefaultSqlConnection>();
            services.AddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();
            services.AddSingleton<IBaseSchemaRunner, BaseSchemaRunner>();
            services.AddSingleton<ISchemaManagerDataStore, SchemaManagerDataStore>();
            services.AddSingleton<ISchemaClient, SchemaClient>();
            services.AddSingleton<ISchemaManager, SqlSchemaManager>();
            services.AddLogging(configure => configure.AddConsole());
            return services.BuildServiceProvider();
        }
    }
}
