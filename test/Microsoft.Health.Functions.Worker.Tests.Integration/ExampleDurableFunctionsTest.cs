// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DurableTask.Client;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

[TestClass]
public class ExampleDurableFunctionsTest : DurableFunctionsTest
{
    private readonly ResiliencePipeline<OrchestrationMetadata?> _pipeline;

    public ExampleDurableFunctionsTest(TestContext testContext)
        : base(testContext)
    {
        _pipeline = new ResiliencePipelineBuilder<OrchestrationMetadata?>()
            .AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMinutes(1) })
            .AddRetry(new RetryStrategyOptions<OrchestrationMetadata?>
            {
                Delay = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = int.MaxValue,
                ShouldHandle = new PredicateBuilder<OrchestrationMetadata?>()
                    .HandleResult(m => m is not null && m.RuntimeStatus.IsInProgress()),
            })
            .Build();
    }

    [TestMethod]
    public async Task GivenWorkerOrchestration_WhenStarting_ThenCompleteSuccessfully()
    {
        string instanceId = await DurableClient.ScheduleNewOrchestrationInstanceAsync(
            "InsertionSortAsync",
            new { Values = new List<int> { 3, 4, 1, 5, 4, 2 } });

        OrchestrationMetadata? metadata = await _pipeline
            .ExecuteAsync(async t => await DurableClient.GetInstanceAsync(instanceId, getInputsAndOutputs: true, cancellation: t));

        Assert.IsNotNull(metadata);
        Assert.AreEqual(OrchestrationRuntimeStatus.Completed, metadata.RuntimeStatus);

        int[]? actual = metadata.ReadOutputAs<int[]>();
        Assert.IsNotNull(actual);
        Assert.IsTrue(actual!.SequenceEqual([5, 4, 4, 3, 2, 1]), $"Received {string.Join(", ", actual)}");
    }
}
