// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using DurableTask.Core;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Operations.Functions.Worker.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

public class FunctionsCoreToolsTestFixture : IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;

    public FunctionsCoreToolsTestFixture(IMessageSink sink)
    {
        // Create the Durable Client
        IServiceCollection services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build());

        _ = services
            .AddOptions<AzureStorageDurableTaskClientOptions>()
            .BindConfiguration(AzureStorageDurableTaskClientOptions.DefaultSectionName)
            .ValidateDataAnnotations();

        _serviceProvider = services
            .AddLogging(b => b.AddXUnit(sink))
            .AddSingleton(sp => sp
                .GetRequiredService<IOptions<AzureStorageDurableTaskClientOptions>>()
                .Value
                .ToOrchestrationServiceSettings())
            .AddSingleton<IOrchestrationServiceClient>(sp => new AzureStorageOrchestrationService(sp.GetRequiredService<AzureStorageOrchestrationServiceSettings>()))
            .AddDurableTaskClient(b => b.UseOrchestrationService())
        .BuildServiceProvider();

        DurableClient = _serviceProvider.GetRequiredService<DurableTaskClient>();

        // Create the host process
        DirectoryInfo testFolder = FindParentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "test");
        string projectFolder = Path.Combine(testFolder.FullName, "Microsoft.Health.Functions.Worker.Examples");

        AzureStorageDurableTaskClientOptions options = _serviceProvider.GetRequiredService<IOptions<AzureStorageDurableTaskClientOptions>>().Value;
        Host = AzureFunctionsProcess.Create(
            projectFolder,
            environment: new Dictionary<string, string?>
            {
                { "AzureWebJobsStorage", options.ConnectionString },
                { "AzureFunctionsJobHost__Extensions__DurableTask__HubName", options.TaskHubName },
                { "AzureFunctionsJobHost__Extensions__DurableTask__StorageProvider__PartitionCount", options.PartitionCount.ToString(CultureInfo.InvariantCulture) },
            },
            enableRaisingEvents: true);
    }

    public DurableTaskClient DurableClient { get; }

    public Process Host { get; }

    public Task DisposeAsync()
    {
        if (!Host.HasExited)
            Host.Kill(entireProcessTree: true);

        return Task.CompletedTask;
    }

    public Task InitializeAsync()
        => Task.CompletedTask;

    public void StartProcess(ITestOutputHelper outputHelper)
    {
        // StartProcess is separate from InitializeAsync so that callers can
        // pass a proper ITestOutputHelper instance that will output in VS.
        ArgumentNullException.ThrowIfNull(outputHelper);

        Host.OutputDataReceived += (sender, args) => outputHelper.WriteLine(args.Data);
        Host.ErrorDataReceived += (sender, args) => outputHelper.WriteLine(args.Data);

        // Attempt to start the process
        if (!Host.Start())
            throw new InvalidOperationException($"Failed to start process '{Host.StartInfo.FileName}'");

        outputHelper.WriteLine("Started Isolated Azure Functions host with PID {0}.", Host.Id);

        // Begin reading the redirected I/O
        Host.BeginOutputReadLine();
        Host.BeginErrorReadLine();
    }

    private static DirectoryInfo FindParentDirectory(string path, string name)
    {
        DirectoryInfo? current = new(path);

        // Search by exact name, regardless of file system
        while (current is not null && current.Name != name)
            current = current.Parent;

        return current ?? throw new DirectoryNotFoundException($"Could not find directory '{name}' in '{path}'.");
    }
}
