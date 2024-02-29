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

public class IDurableContextExtensionsTests
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
    [InlineData("5e81a27e-3bc0-435e-8ff5-a748203e5306")]
    public void GivenInvalidIdInActivity_WhenGettingOperationId_ThenThrowFormatException(string instanceId)
    {
        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Throws<FormatException>(() => context.GetOperationId());
    }

    [Fact]
    public void GivenValidIdInActivity_WhenGettingOperationId_ThenReturnedParsedValue()
    {
        Guid expected = Guid.NewGuid();
        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(expected.ToString(OperationId.FormatSpecifier));

        Assert.Equal(expected, context.GetOperationId());
    }

    [Theory]
    [InlineData("baz")]
    [InlineData("ba9a0977-cc48-4bfc-b4c9-f160f82b888a")]
    public void GivenInvalidIdInOrchestration_WhenGettingOperationId_ThenThrowFormatException(string instanceId)
    {
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(instanceId);

        Assert.Throws<FormatException>(() => context.GetOperationId());
    }

    [Fact]
    public void GivenValidIdInOrchestration_WhenGettingOperationId_ThenReturnedParsedValue()
    {
        Guid expected = Guid.NewGuid();
        IDurableOrchestrationContext context = Substitute.For<IDurableOrchestrationContext>();
        context.InstanceId.Returns(expected.ToString(OperationId.FormatSpecifier));

        Assert.Equal(expected, context.GetOperationId());
    }

    [Fact]
    [Obsolete("GetCreatedTimeAsync deprecated in favor of GetCreatedAtTimeAsync.")]
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
                Arg.Any<GetInstanceStatusOptions>())
            .Returns(new DurableOrchestrationStatus { CreatedTime = expected });

        // Invoke
        DateTime actual = await context.GetCreatedTimeAsync();

        // Assert behavior
        Assert.Equal(expected, actual);

        await context
            .Received(1)
            .CallActivityAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                Arg.Any<GetInstanceStatusOptions>());
    }

    [Fact]
    [Obsolete("GetCreatedTimeAsync deprecated in favor of GetCreatedAtTimeAsync.")]
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
                Arg.Any<GetInstanceStatusOptions>())
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
                Arg.Any<GetInstanceStatusOptions>());
    }

    [Fact]
    public async Task GivenRunningOrchestration_WhenQueryingCreatedAtTime_ThenReturnCreatedAtTime()
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
                Arg.Any<GetInstanceStatusOptions>())
            .Returns(new DurableOrchestrationStatus { CreatedTime = expected });

        // Invoke
        DateTimeOffset actual = await context.GetCreatedAtTimeAsync();

        // Assert behavior
        Assert.Equal(expected, actual);

        await context
            .Received(1)
            .CallActivityAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                Arg.Any<GetInstanceStatusOptions>());
    }

    [Fact]
    public async Task GivenRunningOrchestration_WhenQueryingCreatedAtTimeWithOptions_ThenReturnCreatedAtTime()
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
                Arg.Any<GetInstanceStatusOptions>())
            .Returns(new DurableOrchestrationStatus { CreatedTime = expected });

        // Invoke
        DateTimeOffset actual = await context.GetCreatedAtTimeAsync(options);

        // Assert behavior
        Assert.Equal(expected, actual);

        await context
            .Received(1)
            .CallActivityWithRetryAsync<DurableOrchestrationStatus>(
                nameof(DurableOrchestrationClientActivity.GetInstanceStatusAsync),
                options,
                Arg.Any<GetInstanceStatusOptions>());
    }
}
