// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.SqlServer.Api.Controllers;
using Microsoft.Health.SqlServer.Api.Features;
using Microsoft.Health.SqlServer.Api.Features.Schema;
using Microsoft.Health.SqlServer.Features.Health;

namespace Microsoft.Health.SqlServer.Api.Registration
{
    public static class SqlServerApiRegistrationExtensions
    {
        public static IServiceCollection AddSqlServerApi(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services);

            services.AddMvc()
                .AddApplicationPart(typeof(SchemaController).Assembly);

            services
                .AddHealthChecks()
                .AddCheck<SqlServerHealthCheck>("DataStoreHealthCheck");

            services.Add<CompatibilityVersionHandler>()
                .Transient()
                .AsImplementedInterfaces();

            services.Add<CurrentVersionHandler>()
                .Transient()
                .AsImplementedInterfaces();

            services.AddHostedService<SchemaJobWorkerBackgroundService>();

            return services;
        }
    }
}
