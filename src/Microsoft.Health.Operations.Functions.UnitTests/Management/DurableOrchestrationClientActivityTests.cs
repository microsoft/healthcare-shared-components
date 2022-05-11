// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Operations.Functions.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.Management;

public class DurableOrchestrationClientActivityTests
{
    [Fact]
    public async Task GivenNoInstance_WhenQueryingStatus_ThenReturnNull()
    {
        // Arrange input
        string instanceId = OperationId.Generate();

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(instanceId);
        context.GetInput<GetInstanceStatusOptions>().Returns(new GetInstanceStatusOptions());

        // Note: this scenario should not happen, as an orchestration should be the one invoking this activity!
        IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
        client.GetStatusAsync(instanceId, false, false, false).Returns(Task.FromResult<DurableOrchestrationStatus>(null!));

        // Call activity
        DurableOrchestrationStatus actual = await DurableOrchestrationClientActivity.GetInstanceStatusAsync(context, client, NullLogger.Instance);

        // Assert behavior
        Assert.Null(actual);
        context.Received(1).GetInput<GetInstanceStatusOptions>();
        await client.Received(1).GetStatusAsync(instanceId, false, false, false);
    }

    [Fact]
    public async Task GivenValidInstance_WhenQueryingStatus_ThenReturnStatus()
    {
        // Arrange input
        string instanceId = OperationId.Generate();
        var expected = new DurableOrchestrationStatus { InstanceId = instanceId };

        IDurableActivityContext context = Substitute.For<IDurableActivityContext>();
        context.InstanceId.Returns(instanceId);
        context.GetInput<GetInstanceStatusOptions>().Returns(new GetInstanceStatusOptions { ShowHistory = true, ShowHistoryOutput = true });

        IDurableOrchestrationClient client = Substitute.For<IDurableOrchestrationClient>();
        client.GetStatusAsync(instanceId, true, true, false).Returns(Task.FromResult(expected));

        // Call activity
        DurableOrchestrationStatus actual = await DurableOrchestrationClientActivity.GetInstanceStatusAsync(context, client, NullLogger.Instance);

        // Assert behavior
        Assert.Same(expected, actual);
        context.Received(1).GetInput<GetInstanceStatusOptions>();
        await client.Received(1).GetStatusAsync(instanceId, true, true, false);
    }
}
