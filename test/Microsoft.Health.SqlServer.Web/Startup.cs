// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Api.Registration;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Registration;
using Microsoft.Health.SqlServer.Web.Features.Schema;

namespace Microsoft.Health.SqlServer.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options => { options.EnableEndpointRouting = false; })
                .AddNewtonsoftJson();

            services.AddSqlServerBase<SchemaVersion>(Configuration);
            services.AddSqlServerApi();

            services.AddMediatR(typeof(CompatibilityVersionHandler).Assembly);

            services.Add(provider => new SchemaInformation((int)SchemaVersion.Version1, (int)SchemaVersion.Version2))
                .Singleton()
                .AsSelf()
                .AsImplementedInterfaces();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }
    }
}