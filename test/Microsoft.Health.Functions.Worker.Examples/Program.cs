// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Functions.Worker.Examples.Sorting;
using Microsoft.Health.Operations.Functions.Worker.Management;

IHost host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        _ = services.AddSingleton(TimeProvider.System);
        _ = services
            .AddOptions<PurgeHistoryOptions>()
            .BindConfiguration(PurgeHistoryOptions.SectionName)
            .ValidateDataAnnotations();
        _ = services
            .AddOptions<SortingOptions>()
            .BindConfiguration(SortingOptions.SectionName)
            .ValidateDataAnnotations();
    })
    .ConfigureAppConfiguration(builder => builder.AddJsonFile("worker.json", optional: false))
    .Build();

host.Run();
