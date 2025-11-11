// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using EnsureThat;
using Medino.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Api.Registration;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Registration;
using Microsoft.Health.SqlServer.Web.Features.Schema;

namespace Microsoft.Health.SqlServer.Web.Hosting;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Used by test")]
public static class SqlServerApplicationHostingExtensions
{
    public static IServiceCollection ConfigureSqlServerWebServices(this IServiceCollection services)
    {
        EnsureArg.IsNotNull(services, nameof(services));

        services.AddMvc(options => options.EnableEndpointRouting = false);

        services
            .AddOptions<SqlServerDataStoreConfiguration>()
            .BindConfiguration(SqlServerDataStoreConfiguration.SectionName);

        services
            .AddSqlServerConnection()
            .AddSqlServerManagement<SchemaVersion>()
            .AddSqlServerApi();

        services.AddMedino(c => c.RegisterServicesFromAssemblyContaining<CompatibilityVersionHandler>());
        services.AddSingleton(new SchemaInformation((int)SchemaVersion.Version1, (int)SchemaVersion.Version2));

        return services;
    }

    public static IApplicationBuilder ConfigureSqlServerWebApp(this IApplicationBuilder app)
    {
        EnsureArg.IsNotNull(app, nameof(app));
        return app.UseMvc();
    }
}
