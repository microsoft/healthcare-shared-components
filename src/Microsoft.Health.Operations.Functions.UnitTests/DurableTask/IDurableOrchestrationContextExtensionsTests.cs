// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Health.Operations.Functions.DurableTask;
using Microsoft.Health.Operations.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.DurableTask;

public class IDurableOrchestrationContextExtensionsTests
{
    [Theory]
    [InlineData("foo")]
    [InlineData("{88b1375b-75ca-495d-8c85-28cfe1a94de1}")]
    public void GivenInvalidId_WhenValidating_ThenThrowFormatException(string instanceId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Throws<FormatException>(() => context.ThrowIfInvalidOperationId());
    }

    [Fact]
    public void GivenValidId_WhenValidating_ThenPass()
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(OperationId.Generate());

        context.ThrowIfInvalidOperationId();
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("ba9a0977-cc48-4bfc-b4c9-f160f82b888a")]
    public void GivenInvalidId_WhenGettingOperationId_ThenThrowFormatException(string instanceId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Throws<FormatException>(() => context.GetOperationId());
    }

    [Fact]
    public void GivenValidId_WhenGettingOperationId_ThenReturnedParsedValue()
    {
        Guid expected = Guid.NewGuid();
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(expected.ToString(OperationId.FormatSpecifier));

        Assert.Equal(expected, context.GetOperationId());
    }

    [Fact]
    public async Task GivenRunningOrchestration_WhenQueryingCreatedTime_ThenReturnCreatedTime()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        var expected = DateTime.UtcNow;

        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);

        context
            .CallActivityAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                Arg.Is<GetInstanceStatusInput>(x => x.InstanceId == operationId))
            .Returns(new DurableOrchestrationStatus { CreatedTime = expected });

        // Invoke
        DateTime actual = await context.GetCreatedTimeAsync();

        // Assert behavior
        Assert.Equal(expected, actual);

        await context
            .Received(1)
            .CallActivityAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                Arg.Is<GetInstanceStatusInput>(x => x.InstanceId == operationId));
    }

    [Fact]
    public async Task GivenRunningOrchestration_WhenQueryingCreatedTimeWithOptions_ThenReturnCreatedTime()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        var expected = DateTime.UtcNow;

        string operationId = OperationId.Generate();
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(operationId);

        var options = new RetryOptions(TimeSpan.FromSeconds(5), 3);

        context
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                options,
                Arg.Is<GetInstanceStatusInput>(x => x.InstanceId == operationId))
            .Returns(new DurableOrchestrationStatus { CreatedTime = expected });

        // Invoke
        DateTime actual = await context.GetCreatedTimeAsync(options);

        // Assert behavior
        Assert.Equal(expected, actual);

        await context
            .Received(1)
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                options,
                Arg.Is<GetInstanceStatusInput>(x => x.InstanceId == operationId));
    }
}
