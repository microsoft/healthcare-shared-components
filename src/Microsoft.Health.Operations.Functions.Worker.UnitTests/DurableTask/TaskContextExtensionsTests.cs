// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.DurableTask;
using Microsoft.Health.Operations.Functions.Management;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;
using Microsoft.Health.Operations.Functions.Worker.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.Worker.UnitTests.DurableTask;

public class TaskContextExtensionsTests
{
    [Theory]
    [InlineData("foo")]
    [InlineData("{88b1375b-75ca-495d-8c85-28cfe1a94de1}")]
    public void GivenInvalidId_WhenValidating_ThenThrowFormatException(string instanceId)
    {
        TaskOrchestrationContext context = Substitute.For<TaskOrchestrationContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Throws<FormatException>(context.ThrowIfInvalidOperationId);
    }

    [Fact]
    public void GivenValidId_WhenValidating_ThenPass()
    {
        TaskOrchestrationContext context = Substitute.For<TaskOrchestrationContext>();
        context.InstanceId.Returns(OperationId.Generate());

        context.ThrowIfInvalidOperationId();
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("5e81a27e-3bc0-435e-8ff5-a748203e5306")]
    public void GivenInvalidIdInActivity_WhenGettingOperationId_ThenThrowFormatException(string instanceId)
    {
        TaskActivityContext context = Substitute.For<TaskActivityContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Throws<FormatException>(() => context.GetOperationId());
    }

    [Fact]
    public void GivenValidIdInActivity_WhenGettingOperationId_ThenReturnedParsedValue()
    {
        Guid expected = Guid.NewGuid();
        TaskActivityContext context = Substitute.For<TaskActivityContext>();
        context.InstanceId.Returns(expected.ToString(OperationId.FormatSpecifier));

        Assert.Equal(expected, context.GetOperationId());
    }

    [Theory]
    [InlineData("baz")]
    [InlineData("ba9a0977-cc48-4bfc-b4c9-f160f82b888a")]
    public void GivenInvalidIdInOrchestration_WhenGettingOperationId_ThenThrowFormatException(string instanceId)
    {
        TaskOrchestrationContext context = Substitute.For<TaskOrchestrationContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Throws<FormatException>(() => context.GetOperationId());
    }

    [Fact]
    public void GivenValidIdInOrchestration_WhenGettingOperationId_ThenReturnedParsedValue()
    {
        Guid expected = Guid.NewGuid();
        TaskOrchestrationContext context = Substitute.For<TaskOrchestrationContext>();
        context.InstanceId.Returns(expected.ToString(OperationId.FormatSpecifier));

        Assert.Equal(expected, context.GetOperationId());
    }

    [Fact]
    public async Task GivenRunningOrchestration_WhenQueryingCreatedAtTime_ThenReturnCreatedAtTime()
    {
        // Arrange input
        string operationId = OperationId.Generate();
        var expected = DateTime.UtcNow;

        TaskOrchestrationContext context = Substitute.For<TaskOrchestrationContext>();
        TaskOptions taskOptions = new(new TaskRetryOptions(new RetryPolicy(1, TimeSpan.FromSeconds(1))));
        context.InstanceId.Returns(operationId);

        context
            .CallActivityAsync<OrchestrationInstanceMetadata?>(default, default, default)
            .ReturnsForAnyArgs(new OrchestrationInstanceMetadata("MyOrchestration", operationId) { CreatedAt = expected });

        // Invoke
        DateTimeOffset actual = await context.GetCreatedAtTimeAsync(taskOptions);

        // Assert behavior
        Assert.Equal(expected, actual);

        await context
            .Received(1)
            .CallActivityAsync<OrchestrationInstanceMetadata?>(
                nameof(DurableTaskClientActivity.GetInstanceAsync),
                Arg.Is<GetInstanceOptions>(x => !x.GetInputsAndOutputs),
                Arg.Is(taskOptions));
    }
}
