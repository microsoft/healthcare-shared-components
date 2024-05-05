// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.Core;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

public class FunctionsCoreToolsTestFixture : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;

    private static readonly ResiliencePipeline<HttpResponseMessage> HealthCheckPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMinutes(3) })
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            Delay = TimeSpan.FromSeconds(1),
            MaxRetryAttempts = int.MaxValue,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleInner<HttpRequestException>()
                .HandleResult(m => !m.IsSuccessStatusCode),
        })
        .Build();

    public FunctionsCoreToolsTestFixture(IMessageSink sink)
    {
        // Create the Durable Client
        IServiceCollection services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build());

        _ = services
            .AddOptions<FunctionWorkerOptions>()
            .BindConfiguration(FunctionWorkerOptions.DefaultSectionName)
            .ValidateDataAnnotations();

        _serviceProvider = services
            .AddLogging(b => b.AddXUnit(sink))
            .AddSingleton(sp => sp
                .GetRequiredService<IOptions<FunctionWorkerOptions>>()
                .Value
                .DurableTask
                .ToOrchestrationServiceSettings())
            .AddSingleton<IOrchestrationServiceClient>(sp => new AzureStorageOrchestrationService(sp.GetRequiredService<AzureStorageOrchestrationServiceSettings>()))
            .AddDurableTaskClient(b => b.UseOrchestrationService())
        .BuildServiceProvider();

        DurableClient = _serviceProvider.GetRequiredService<DurableTaskClient>();
    }

    public DurableTaskClient DurableClient { get; }

    public Task DisposeAsync()
        => Task.CompletedTask;

    public Task InitializeAsync()
    {
        // Wait for host to start at an address like http://localhost:7071/api/healthz
        FunctionWorkerOptions options = _serviceProvider.GetRequiredService<IOptions<FunctionWorkerOptions>>().Value;
        UriBuilder builder = new()
        {
            Scheme = "http://",
            Host = "localhost",
            Port = options.Port,
            Path = "api"
        };

        using HttpClient client = new() { BaseAddress = builder.Uri };
        Uri healthCheck = new("/healthz", UriKind.Relative);
        return HealthCheckPipeline.ExecuteAsync(async t => await client.GetAsync(healthCheck, t)).AsTask();
    }
}
