// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Operations.Functions.Management;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.Management;

public class DurableOrchestrationClientActivityTests
{
    [Fact]
    [Obsolete("GetInstanceStatusAsync deprecated in favor of GetInstanceAsync.")]
    public async Task GivenNoInstance_WhenQueryingStatus_ThenReturnNull()
    {
        // Arrange input
        string instanceId = OperationId.Generate();

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(instanceId);
        context.GetInput<GetInstanceStatusOptions>().Returns(new GetInstanceStatusOptions());

        // Note: this scenario should not happen, as an orchestration should be the one invoking this activity!
        IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
        client.GetStatusAsync(default(string), default, default, default).ReturnsForAnyArgs(Task.FromResult<DurableOrchestrationStatus>(null!));

        // Call activity
        DurableOrchestrationMetadata? actual = await DurableOrchestrationClientActivity.GetInstanceStatusAsync(context, client, NullLogger.Instance);

        // Assert behavior
        Assert.Null(actual);
        context.Received(1).GetInput<GetInstanceStatusOptions>();
        await client.Received(1).GetStatusAsync(instanceId, false, false, false);
    }

    [Fact]
    [Obsolete("GetInstanceStatusAsync deprecated in favor of GetInstanceAsync.")]
    public async Task GivenValidInstance_WhenQueryingStatus_ThenReturnStatus()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        var expected = new DurableOrchestrationStatus { InstanceId = instanceId };

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(instanceId);
        context.GetInput<GetInstanceStatusOptions>().Returns(new GetInstanceStatusOptions { GetInputsAndOutputs = true, ShowHistory = false });

        IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
        client.GetStatusAsync(default(string), default, default, default).ReturnsForAnyArgs(Task.FromResult(expected));

        // Call activity
        DurableOrchestrationMetadata? actual = await DurableOrchestrationClientActivity.GetInstanceStatusAsync(context, client, NullLogger.Instance);

        // Assert behavior
        AssertEqual(expected, actual);
        context.Received(1).GetInput<GetInstanceStatusOptions>();
        await client.Received(1).GetStatusAsync(instanceId, false, true, true);
    }

    [Fact]
    public async Task GivenNoInstance_WhenQueryingInstance_ThenReturnNull()
    {
        // Arrange input
        string instanceId = OperationId.Generate();

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(instanceId);
        context.GetInput<GetInstanceOptions>().Returns(new GetInstanceOptions());

        // Note: this scenario should not happen, as an orchestration should be the one invoking this activity!
        IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
        client.GetStatusAsync(default(string), default, default, default).ReturnsForAnyArgs(Task.FromResult<DurableOrchestrationStatus>(null!));

        // Call activity
        OrchestrationInstanceMetadata? actual = await DurableOrchestrationClientActivity.GetInstanceAsync(context, client, NullLogger.Instance);

        // Assert behavior
        Assert.Null(actual);
        context.Received(1).GetInput<GetInstanceOptions>();
        await client.Received(1).GetStatusAsync(instanceId, false, false, false);
    }

    [Fact]
    public async Task GivenValidInstance_WhenQueryingInstance_ThenReturnStatus()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        var expected = new DurableOrchestrationStatus
        {
            CreatedTime = DateTime.UtcNow.AddMinutes(-15),
            CustomStatus = JToken.Parse("\"foo\""),
            Input = JToken.Parse("[1,2,3]"),
            InstanceId = instanceId,
            LastUpdatedTime = DateTime.UtcNow,
            Name = "bar",
            Output = JToken.Parse("6"),
            RuntimeStatus = OrchestrationRuntimeStatus.Completed,
        };

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(instanceId);
        context.GetInput<GetInstanceOptions>().Returns(new GetInstanceOptions { GetInputsAndOutputs = true });

        IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
        client.GetStatusAsync(default(string), default, default, default).ReturnsForAnyArgs(Task.FromResult(expected));

        // Call activity
        OrchestrationInstanceMetadata? actual = await DurableOrchestrationClientActivity.GetInstanceAsync(context, client, NullLogger.Instance);

        // Assert behavior
        AssertEqual(expected, actual);
        context.Received(1).GetInput<GetInstanceOptions>();
        await client.Received(1).GetStatusAsync(instanceId, false, true, true);
    }

    [Obsolete("GetInstanceStatusAsync deprecated in favor of GetInstanceAsync.")]
    private static void AssertEqual(DurableOrchestrationStatus expected, DurableOrchestrationMetadata? actual)
    {
        Assert.NotNull(actual);

        Assert.Same(expected.InstanceId, actual.InstanceId);
        Assert.Same(expected.Name, actual.Name);
        Assert.Equal(expected.CreatedTime, actual.CreatedTime);
        Assert.Equal(expected.LastUpdatedTime, actual.LastUpdatedTime);
        Assert.Equal(expected.RuntimeStatus, actual.RuntimeStatus);
        Assert.Same(expected.Input, actual.Input);
        Assert.Same(expected.Output, actual.Output);
        Assert.Same(expected.CustomStatus, actual.CustomStatus);
    }

    private static void AssertEqual(DurableOrchestrationStatus expected, OrchestrationInstanceMetadata? actual)
    {
        Assert.NotNull(actual);

        Assert.Equal(expected.InstanceId, actual.InstanceId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.CreatedTime, actual.CreatedAt);
        Assert.Equal(expected.LastUpdatedTime, actual.LastUpdatedAt);
        Assert.Equal(expected.RuntimeStatus, actual.RuntimeStatus);
        Assert.Equal(expected.Input?.ToString(Newtonsoft.Json.Formatting.None), actual.SerializedInput);
        Assert.Equal(expected.Output?.ToString(Newtonsoft.Json.Formatting.None), actual.SerializedOutput);
        Assert.Equal(expected.CustomStatus?.ToString(Newtonsoft.Json.Formatting.None), actual.SerializedCustomStatus);
    }
}
