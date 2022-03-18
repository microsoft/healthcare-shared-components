// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Health.Functions.Examples;
using Microsoft.Health.Functions.Examples.Sorting;
using Xunit;

namespace Microsoft.Health.Functions.Tests.Integration;

public class DistributedSorterTests : IClassFixture<WebJobsTestFixture<Startup>>
{
    private readonly IDurableClient _durableClient;

    public DistributedSorterTests(IDurableClientFactory factory)
        => _durableClient = EnsureArg.IsNotNull(factory, nameof(factory)).CreateClient();

    [Fact]
    public async Task GivenUnsortedList_WhenSortingViaOrchestration_ThenSuccessfullySort()
    {
        string instanceId = await _durableClient.StartNewAsync(
            nameof(DistributedSorter.InsertionSortAsync),
            new SortingInput { Values = new int[] { 3, 4, 1, 5, 2 } });

        DurableOrchestrationStatus status = await _durableClient.GetStatusAsync(instanceId);
        while (IsRunning(status.RuntimeStatus))
        {
            await Task.Delay(1000);
            status = await _durableClient.GetStatusAsync(instanceId);
        }

        Assert.Equal(OrchestrationRuntimeStatus.Completed, status.RuntimeStatus);
    }

    private static bool IsRunning(OrchestrationRuntimeStatus runtimeStatus)
        => runtimeStatus == OrchestrationRuntimeStatus.Pending
        || runtimeStatus == OrchestrationRuntimeStatus.Running
        || runtimeStatus == OrchestrationRuntimeStatus.ContinuedAsNew;

}
