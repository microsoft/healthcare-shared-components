// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using Microsoft.DurableTask.Client;
using Microsoft.Health.Functions.Worker.Examples.Sorting;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;
using Xunit;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

public class DurableFunctionsTest()
{
    private readonly DurableTaskClient _durableClient = null!;

    [Fact(Skip = "The fixture needs to be implemented to run this locally.")]
    public async Task GivenOrchestration_WhenStarting_ThenCompleteSuccessfully()
    {
        string instanceId = await _durableClient
            .ScheduleNewOrchestrationInstanceAsync(
                nameof(DistributedSorter.InsertionSortAsync),
                new SortingInput([3, 4, 1, 5, 4, 2]));

        OrchestrationMetadata? status = await _durableClient.GetInstanceAsync(instanceId);
        while (status is not null && status.RuntimeStatus.IsInProgress())
        {
            await Task.Delay(1000);
            status = await _durableClient.GetInstanceAsync(instanceId);
        }

        Assert.NotNull(status);
        Assert.Equal(OrchestrationRuntimeStatus.Completed, status.RuntimeStatus);

        int[]? actual = status.ReadOutputAs<int[]>();
        Assert.NotNull(actual);
        Assert.True(actual!.SequenceEqual([5, 4, 4, 3, 2, 1]));
    }
}
