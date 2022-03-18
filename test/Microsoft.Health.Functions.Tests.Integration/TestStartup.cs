// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Functions.Tests.Integration;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used by test framework.")]
public class TestStartup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration(b => b
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables());
    }

    public void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        services.AddDurableClientFactory(x => context.Configuration.GetSection("DurableTask").Bind(x));
        services.Replace(ServiceDescriptor.Singleton<IMessageSerializerSettingsFactory, DurableTaskSerializerSettingsFactory>());
    }
}
