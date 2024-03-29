// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Operations.Functions.Management;
using Microsoft.Health.Operations.Functions.Worker.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.Worker.UnitTests.Management;

public class DurableOrchestrationClientActivityTests
{
    [Fact]
    public async Task GivenNoInstance_WhenQueryingInstance_ThenReturnNull()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        TaskActivityContext context = Substitute.For<TaskActivityContext>();
        DurableTaskClient client = Substitute.For<DurableTaskClient>("TestTaskHub");
        GetInstanceOptions options = new() { GetInputsAndOutputs = true };

        // Note: Returning null shouldn't be possible in practice
        context.InstanceId.Returns(instanceId);
        client.GetInstanceAsync(default!, default, default).ReturnsForAnyArgs(Task.FromResult<OrchestrationMetadata?>(null));

        // Call activity
        using CancellationTokenSource cts = new();
        OrchestrationInstanceMetadata? actual = await DurableTaskClientActivity.GetInstanceAsync(context, client, options, NullLogger.Instance, cts.Token);

        // Assert behavior
        Assert.Null(actual);
        await client
            .Received(1)
            .GetInstanceAsync(instanceId, options.GetInputsAndOutputs, cts.Token);
    }

    [Fact]
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
        OrchestrationInstanceMetadata? actual = await DurableTaskClientActivity.GetInstanceAsync(context, client, options, NullLogger.Instance, cts.Token);

        // Assert behavior
        AssertEqual(expected, actual);
        await client
            .Received(1)
            .GetInstanceAsync(instanceId, options.GetInputsAndOutputs, cts.Token);
    }

    private static void AssertEqual(OrchestrationMetadata? expected, OrchestrationInstanceMetadata? actual)
    {
        Assert.NotNull(actual);

        Assert.Equal(expected!.InstanceId, actual.InstanceId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.LastUpdatedAt, actual.LastUpdatedAt);
        Assert.Equal(expected.RuntimeStatus, actual.RuntimeStatus);
        Assert.Equal(expected.SerializedCustomStatus, actual.SerializedCustomStatus);
        Assert.Equal(expected.SerializedInput, actual.SerializedInput);
        Assert.Equal(expected.SerializedOutput, actual.SerializedOutput);
    }
}
