// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Operations.Functions.Management;
using Microsoft.Health.Operations.Functions.Worker.Management;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Operations.Functions.Worker.UnitTests.Management;

[TestClass]
public class DurableOrchestrationClientActivityTests
{
    private readonly FunctionContext _context;

    public DurableOrchestrationClientActivityTests()
    {
        _context = Substitute.For<FunctionContext>();
        _context
            .InstanceServices
            .Returns(new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider());
    }

    [TestMethod]
    public async Task GivenNoInstance_WhenQueryingInstance_ThenReturnNull()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        GetInstanceOptions options = new() { GetInputsAndOutputs = true };
        DurableTaskClient client = Substitute.For<DurableTaskClient>("TestTaskHub");

        // Note: Returning null shouldn't be possible in practice
        client.GetInstanceAsync(default!, default, default).ReturnsForAnyArgs(Task.FromResult<OrchestrationMetadata?>(null));

        // Call activity
        using CancellationTokenSource cts = new();
        OrchestrationInstanceMetadata? actual = await DurableTaskClientActivity.GetInstanceAsync(options, client, _context, instanceId, cts.Token);

        // Assert behavior
        Assert.IsNull(actual);
        await client
            .Received(1)
            .GetInstanceAsync(instanceId, options.GetInputsAndOutputs, cts.Token);
    }

    [TestMethod]
    public async Task GivenValidInstance_WhenQueryingInstance_ThenReturnStatus()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        TaskActivityContext context = Substitute.For<TaskActivityContext>();
        DurableTaskClient client = Substitute.For<DurableTaskClient>("TestTaskHub");
        GetInstanceOptions options = new() { GetInputsAndOutputs = true };
        OrchestrationMetadata expected = new("MyOrchestration", instanceId)
        {
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-15),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
            SerializedCustomStatus = "{ \"hello\": \"world\" }",
            SerializedInput = "{ \"input\": 5 }",
            SerializedOutput = "\"five\"",
        };

        // Note: Returning null shouldn't be possible in practice
        context.InstanceId.Returns(instanceId);
        client
            .GetInstanceAsync(default!, default, default)
            .ReturnsForAnyArgs(Task.FromResult<OrchestrationMetadata?>(expected));

        // Call activity
        using CancellationTokenSource cts = new();
        OrchestrationInstanceMetadata? actual = await DurableTaskClientActivity.GetInstanceAsync(options, client, _context, instanceId, cts.Token);

        // Assert behavior
        AssertEqual(expected, actual);
        await client
            .Received(1)
            .GetInstanceAsync(instanceId, options.GetInputsAndOutputs, cts.Token);
    }

    private static void AssertEqual(OrchestrationMetadata? expected, OrchestrationInstanceMetadata? actual)
    {
        Assert.IsNotNull(actual);

        Assert.AreEqual(expected!.InstanceId, actual.InstanceId);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.CreatedAt, actual.CreatedAt);
        Assert.AreEqual(expected.LastUpdatedAt, actual.LastUpdatedAt);
        Assert.AreEqual(expected.RuntimeStatus, actual.RuntimeStatus);
        Assert.AreEqual(expected.SerializedCustomStatus, actual.SerializedCustomStatus);
        Assert.AreEqual(expected.SerializedInput, actual.SerializedInput);
        Assert.AreEqual(expected.SerializedOutput, actual.SerializedOutput);
    }
}
