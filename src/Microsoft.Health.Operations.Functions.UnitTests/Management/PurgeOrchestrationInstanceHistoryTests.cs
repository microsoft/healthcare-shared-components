// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
#if NET8_0_OR_GREATER
using Microsoft.Extensions.Time.Testing;
#else
using Microsoft.Health.Core.Internal;
#endif
using Microsoft.Health.Operations.Functions.Management;
#if !NET8_0_OR_GREATER
using Microsoft.Health.Test.Utilities;
#endif
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.Management;

public class PurgeOrchestrationInstanceHistoryTests
{
#if NET8_0_OR_GREATER
    private readonly FakeTimeProvider _timeProvider = new(UtcNow);
#endif
    private readonly TimerInfo _timer;
    private readonly PurgeHistoryOptions _purgeConfig;
    private readonly IDurableOrchestrationClient _durableClient;
    private readonly PurgeOrchestrationInstanceHistory _purgeTask;

    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;

    public PurgeOrchestrationInstanceHistoryTests()
    {
        _timer = Substitute.For<TimerInfo>(default, default, default);
        _purgeConfig = new PurgeHistoryOptions
        {
            Statuses = new HashSet<OrchestrationRuntimeStatus> { OrchestrationRuntimeStatus.Completed },
            MinimumAgeDays = 14,
        };
        _durableClient = Substitute.For<IDurableOrchestrationClient>();
#if NET8_0_OR_GREATER
        _purgeTask = new PurgeOrchestrationInstanceHistory(_timeProvider, Options.Create(_purgeConfig));
#else
        _purgeTask = new PurgeOrchestrationInstanceHistory(Options.Create(_purgeConfig));
#endif
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public async Task GivenOrchestrationInstances_WhenPurgeCompletedDurableFunctionsHistory_ThenOrchestrationsPurgedAsync(int count)
    {
        var instanceId = Guid.NewGuid().ToString();

        var durableOrchestrationState = Enumerable.Repeat(new DurableOrchestrationStatus { InstanceId = instanceId }, count);

        _durableClient
            .ListInstancesAsync(Arg.Is<OrchestrationStatusQueryCondition>(condition => AreConditionEqual(condition)), Arg.Any<CancellationToken>())
            .Returns(new OrchestrationStatusQueryResult
            {
                DurableOrchestrationState = durableOrchestrationState
            });

        _durableClient
            .PurgeInstanceHistoryAsync(instanceId)
            .Returns(new PurgeHistoryResult(count));

#if !NET8_0_OR_GREATER
        using IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => UtcNow);
#endif
        await _purgeTask.Run(_timer, _durableClient, NullLogger.Instance);

        await _durableClient
            .Received(count)
            .PurgeInstanceHistoryAsync(instanceId);
    }

    [Fact]
    public async Task GivenOrchestrationInstancesAndInstancesToSkipPurging_WhenPurgeCompletedDurableFunctionsHistory_ThenNoOrchestrationsPurgedAsync()
    {
        var instanceId1 = Guid.NewGuid().ToString();
        var instanceId2 = Guid.NewGuid().ToString();
        var instanceName1 = "Re-Index";
        var instanceName2 = "CopyInstance";

        _purgeConfig.ExcludeFunctions = new string[] { instanceName2 };

        var durableOrchestrationState = new List<DurableOrchestrationStatus>
        {
            new DurableOrchestrationStatus { InstanceId = instanceId1, Name = instanceName1 },
            new DurableOrchestrationStatus { InstanceId = instanceId2, Name = instanceName2 }
        };

        _durableClient
            .ListInstancesAsync(Arg.Is<OrchestrationStatusQueryCondition>(condition => AreConditionEqual(condition)), Arg.Any<CancellationToken>())
            .Returns(new OrchestrationStatusQueryResult
            {
                DurableOrchestrationState = durableOrchestrationState
            });

        _durableClient
            .PurgeInstanceHistoryAsync(instanceId1)
            .Returns(new PurgeHistoryResult(1));

#if !NET8_0_OR_GREATER
        using IDisposable replacement = Mock.Property(() => ClockResolver.UtcNowFunc, () => UtcNow);
#endif
        await _purgeTask.Run(_timer, _durableClient, NullLogger.Instance);

        await _durableClient
            .Received(1)
            .PurgeInstanceHistoryAsync(instanceId1);
    }

    private bool AreConditionEqual(OrchestrationStatusQueryCondition condition)
    {
        return condition.RuntimeStatus.SequenceEqual(_purgeConfig.Statuses!)
            && condition.CreatedTimeFrom == DateTime.MinValue
            && condition.CreatedTimeTo == UtcNow.AddDays(-_purgeConfig.MinimumAgeDays);
    }
}
