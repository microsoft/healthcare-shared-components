using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Health.SchemaManager.Core;
using Microsoft.Health.SqlServer;
using Microsoft.Health.SqlServer.Features.Schema.Manager;

namespace Microsoft.Health.SchemaManager.Registration
{
    /// <summary>
    /// Contains the collection extensions for adding the Schema Manager.
    /// </summary>
    public static class SchemaManagerRegistrationExtensions
    {
        /// <summary>
        /// Adds the Schema Manager to the DI container. These are resolved when the commands are registered with the
        /// <c>CommandLineBuilder</c>.
        /// </summary>
        /// <param name="services">The service collection to add to.</param>
        /// <returns>The service collection, for chaining.</returns>
        /// <remarks>
        /// We are using convention to register the commands; essentially everything in the same namespace as the
        /// added in other namespaces, this method will need to be modified/extended to deal with that.
        /// </remarks>
        public static IServiceCollection AddSchemaManager(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddHttpClient();
            services.AddSingleton<ISqlConnectionFactory, DefaultSqlConnectionFactory>();
            services.AddSingleton<ISqlConnectionStringProvider, DefaultSqlConnectionStringProvider>();
            services.AddSingleton<IBaseSchemaRunner, BaseSchemaRunner>();
            services.AddSingleton<ISchemaManagerDataStore, SchemaManagerDataStore>();
            services.AddSingleton<ISchemaClient, SchemaClient>();
            services.AddSingleton<ISchemaManager, SqlSchemaManager>();
            services.AddLogging(configure => configure.AddConsole());
            return services;
        }
    }
}
