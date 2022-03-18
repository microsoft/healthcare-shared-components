// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Functions.Examples.Sorting;
using Microsoft.Health.Operations.Functions;

[assembly: FunctionsStartup(typeof(Microsoft.Health.Functions.Examples.Startup))]
namespace Microsoft.Health.Functions.Examples;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));

        IConfiguration config = builder.GetHostConfiguration();
        ConfigureAnnotatedOptions<PurgeHistoryOptions>(builder.Services, config.GetSection(PurgeHistoryOptions.SectionName));
        ConfigureAnnotatedOptions<SortingOptions>(builder.Services, config.GetSection(SortingOptions.SectionName));
    }

    private static void ConfigureAnnotatedOptions<T>(IServiceCollection services, IConfiguration config)
        where T : class
    {
        services
            .AddOptions<T>()
            .Bind(config)
            .ValidateDataAnnotations();
    }
}
