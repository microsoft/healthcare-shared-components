// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.Core;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Functions.Worker.Examples;
using Microsoft.Health.Functions.Worker.Examples.Sorting;
using Microsoft.Health.Functions.Worker.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

public sealed class IsolatedWebJobsTestFixture : IAsyncLifetime
{
    private readonly IHost _jobHost;
    private readonly IHost _workerHost;

    public IsolatedWebJobsTestFixture(IMessageSink sink)
    {
        ServiceCollection services = new();

        _ = services
            .AddOptions<AzureStorageDurableTaskClientOptions>()
            .BindConfiguration(AzureStorageDurableTaskClientOptions.DefaultSectionName)
            .ValidateDataAnnotations();

        Client = services
            .AddLogging(b => b.AddXUnit(sink))
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddEnvironmentVariables().Build())
            .AddSingleton(sp => sp
                .GetRequiredService<IOptions<AzureStorageDurableTaskClientOptions>>()
                .Value
                .ToOrchestrationServiceSettings())
            .AddSingleton<IOrchestrationServiceClient>(sp => new AzureStorageOrchestrationService(sp.GetRequiredService<AzureStorageOrchestrationServiceSettings>()))
            .AddDurableTaskClient(b => b.UseOrchestrationService())
            .BuildServiceProvider()
            .GetRequiredService<DurableTaskClient>();

        _workerHost = new ExampleHostBuilder()
            .ConfigureLogging(b => b.AddXUnit(sink))
            .Build();

        _jobHost = AzureFunctionsJobHostBuilder
            .Create(typeof(DistributedSorter).Assembly)
            .ConfigureLogging(b => b.AddXUnit(sink))
            .ConfigureWebJobs(b => b.AddDurableTask())
            .Build();
    }

    public DurableTaskClient Client { get; }

    public async Task DisposeAsync()
    {
        await _workerHost.StopAsync();
        await _jobHost.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await _workerHost.StartAsync();
        await _jobHost.StartAsync();
    }
}
