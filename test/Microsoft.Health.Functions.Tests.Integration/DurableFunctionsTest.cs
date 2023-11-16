// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Health.Functions.Examples;
using Microsoft.Health.Functions.Examples.Sorting;
using Microsoft.Health.Operations.Functions.DurableTask;
using Xunit;

namespace Microsoft.Health.Functions.Tests.Integration;

public class DurableFunctionsTest : IClassFixture<WebJobsTestFixture<Startup>>
{
    private readonly IDurableClient _durableClient;

    private static readonly int[] OutOfOrder = [3, 4, 1, 5, 4, 2];
    private static readonly int[] Ordered = [5, 4, 4, 3, 2, 1];

    public DurableFunctionsTest(IDurableClientFactory factory)
        => _durableClient = EnsureArg.IsNotNull(factory, nameof(factory)).CreateClient();

    [Fact]
    public async Task GivenOrchestration_WhenStarting_ThenCompleteSuccessfully()
    {
        string instanceId = await _durableClient
            .StartNewAsync(
                nameof(DistributedSorter.InsertionSortAsync),
                new SortingInput(OutOfOrder));

        DurableOrchestrationStatus status = await _durableClient.GetStatusAsync(instanceId);
        while (status.RuntimeStatus.IsInProgress())
        {
            await Task.Delay(1000);
            status = await _durableClient.GetStatusAsync(instanceId);
        }

        Assert.Equal(OrchestrationRuntimeStatus.Completed, status.RuntimeStatus);

        int[]? actual = status.Output.ToObject<int[]>();

        Assert.NotNull(actual);
        Assert.True(Ordered.SequenceEqual(actual!));
    }
}
