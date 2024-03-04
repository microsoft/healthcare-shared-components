// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        string contentRoot = Path.GetDirectoryName(typeof(ExampleHostBuilder).Assembly.Location)!;

        _jobHost = AzureFunctionsJobHostBuilder
            .Create(contentRoot)
            .ConfigureEnvironmentVariables(
                ("Logging:LogLevel:Default", "Debug"),
                ("AzureWebJobsFeatureFlags", "EnableWorkerIndexing"),
                ("LanguageWorkers:WorkersDirectory", contentRoot))
            .ConfigureLogging(b => b.AddXUnit(sink))
            .ConfigureWebJobs(b => b.AddDurableTask())
            .Build();

        _workerHost = new ExampleHostBuilder()
            .ConfigureLogging(b => b.AddXUnit(sink))
            .ConfigureAppConfiguration(x => x.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>("Logging:LogLevel:Default", "Debug"),
                    new KeyValuePair<string, string?>("Functions:Worker:HostEndpoint", "http://localhost:63198"),
                    new KeyValuePair<string, string?>("Functions:Worker:RequestId", Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string?>("Functions:Worker:WorkerId", Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string?>("Functions:Worker:GrpcMaxMessageLength", int.MaxValue.ToString(CultureInfo.InvariantCulture)),
                ]))
            .UseContentRoot(contentRoot)
            .Build();
    }

    public DurableTaskClient Client { get; }

    public async Task DisposeAsync()
    {
        await _jobHost.StopAsync();
        await _workerHost.StopAsync();
    }

    public async Task InitializeAsync()
    {
        await _jobHost.StartAsync();
        await _workerHost.StartAsync();
    }
}
