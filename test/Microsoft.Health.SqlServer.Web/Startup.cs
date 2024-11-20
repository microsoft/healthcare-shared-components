// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Api.Registration;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Registration;
using Microsoft.Health.SqlServer.Web.Features.Schema;

namespace Microsoft.Health.SqlServer.Web;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Used by others.")]
public sealed class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(options => options.EnableEndpointRouting = false);

        services
            .AddSqlServerConnection(c => Configuration.GetSection(SqlServerDataStoreConfiguration.SectionName).Bind(c))
            .AddSqlServerManagement<SchemaVersion>()
            .AddSqlServerApi();

        services.AddMediatR(c => c.RegisterServicesFromAssembly(typeof(CompatibilityVersionHandler).Assembly));

        services
            .Add(provider => new SchemaInformation((int)SchemaVersion.Version1, (int)SchemaVersion.Version2))
            .Singleton()
            .AsSelf()
            .AsImplementedInterfaces();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Startup method.")]
    public void Configure(IApplicationBuilder app)
    {
        app.UseMvc();
    }
}
