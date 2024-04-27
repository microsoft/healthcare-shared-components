// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DurableTask.Client;
using Microsoft.Health.Functions.Worker.Examples.Sorting;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

public class DurableFunctionsTest : IClassFixture<FunctionsCoreToolsTestFixture>
{
    private readonly FunctionsCoreToolsTestFixture _fixture;
    private readonly ITestOutputHelper _outputHelper;
    private readonly ResiliencePipeline<OrchestrationMetadata?> _pipeline;

    public DurableFunctionsTest(FunctionsCoreToolsTestFixture fixture, ITestOutputHelper outputHelper)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(outputHelper);

        _fixture = fixture;
        _outputHelper = outputHelper;
        _pipeline = new ResiliencePipelineBuilder<OrchestrationMetadata?>()
            .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(30) })
            .AddRetry(new RetryStrategyOptions<OrchestrationMetadata?>
            {
                Delay = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = int.MaxValue,
                ShouldHandle = new PredicateBuilder<OrchestrationMetadata?>()
                    .HandleResult(m =>
                        !_fixture.Host.HasExited &&
                        m is not null &&
                        m.RuntimeStatus.IsInProgress()),
            })
            .Build();
    }

    [Fact]
    public async Task GivenOrchestration_WhenStarting_ThenCompleteSuccessfully()
    {
        _fixture.StartProcess(_outputHelper);

        string instanceId = await _fixture.DurableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(DistributedSorter.InsertionSortAsync),
            new SortingInput([3, 4, 1, 5, 4, 2]));

        OrchestrationMetadata? metadata = await _pipeline
            .ExecuteAsync(async t => await _fixture.DurableClient.GetInstanceAsync(instanceId, getInputsAndOutputs: true, cancellation: t));

        Assert.NotNull(metadata);
        Assert.Equal(OrchestrationRuntimeStatus.Completed, metadata.RuntimeStatus);

        int[]? actual = metadata.ReadOutputAs<int[]>();
        Assert.NotNull(actual);
        Assert.True(actual!.SequenceEqual([5, 4, 4, 3, 2, 1]), $"Received {string.Join(", ", actual)}");
    }
}
