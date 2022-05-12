﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Internal;
using Microsoft.Health.Operations.Functions.Management;
using Microsoft.Health.Test.Utilities;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.UnitTests.Management;

public class PurgeOrchestrationInstanceHistoryTests
{
    private readonly DateTime _utcNow;
    private readonly TimerInfo _timer;
    private readonly PurgeHistoryOptions _purgeConfig;
    private readonly IDurableOrchestrationClient _durableClient;
    private PurgeOrchestrationInstanceHistory _purgeTask;

    public PurgeOrchestrationInstanceHistoryTests()
    {
        _utcNow = DateTime.UtcNow;
        _timer = Substitute.For<TimerInfo>(default, default, default);
        _purgeConfig = new PurgeHistoryOptions
        {
            Statuses = new HashSet<OrchestrationRuntimeStatus> { OrchestrationRuntimeStatus.Completed },
            MinimumAgeDays = 14,
        };
        _purgeTask = new PurgeOrchestrationInstanceHistory(Options.Create(_purgeConfig));
        _durableClient = Substitute.For<IDurableOrchestrationClient>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public async Task GivenOrchestrationInstances_WhenPurgeCompletedDurableFunctionsHistory_ThenOrchestrationsPurgedAsync(int count)
    {
        OrchestrationStatusQueryCondition condition = new OrchestrationStatusQueryCondition
        {
            CreatedTimeFrom = DateTime.MinValue,
            CreatedTimeTo = _utcNow.AddDays(-_purgeConfig.MinimumAgeDays),
            RuntimeStatus = _purgeConfig.Statuses,
        };
        var instanceId = Guid.NewGuid().ToString();

        var durableOrchestrationState = Enumerable.Repeat(new DurableOrchestrationStatus { InstanceId = instanceId }, count);

        _durableClient
            .ListInstancesAsync(condition, Arg.Any<CancellationToken>())
            .Returns(new OrchestrationStatusQueryResult
            {
                DurableOrchestrationState = durableOrchestrationState
            });

        _durableClient
            .PurgeInstanceHistoryAsync(instanceId)
            .Returns(new PurgeHistoryResult(count));

        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => _utcNow))
        {
            await _purgeTask.Run(_timer, _durableClient, NullLogger.Instance);
        }

        await _durableClient
            .Received(count)
            .PurgeInstanceHistoryAsync(instanceId);
    }

    [Fact]
    public async Task GivenOrchestrationInstancesAndInstancesToSkipPurging_WhenPurgeCompletedDurableFunctionsHistory_ThenNoOrchestrationsPurgedAsync()
    {
        OrchestrationStatusQueryCondition condition = new OrchestrationStatusQueryCondition
        {
            CreatedTimeFrom = DateTime.MinValue,
            CreatedTimeTo = _utcNow.AddDays(-_purgeConfig.MinimumAgeDays),
            RuntimeStatus = _purgeConfig.Statuses,
        };

        var instanceId1 = Guid.NewGuid().ToString();
        var instanceId2 = Guid.NewGuid().ToString();

        _purgeConfig.InstancesToSkipPurging = new string[] { instanceId1 };
        _purgeTask = new PurgeOrchestrationInstanceHistory(Options.Create(_purgeConfig));

        var durableOrchestrationState = new List<DurableOrchestrationStatus> {
            new DurableOrchestrationStatus { InstanceId = instanceId1, Name = instanceId1 },
            new DurableOrchestrationStatus { InstanceId = instanceId2, Name = instanceId2 }
        };

        _durableClient
            .ListInstancesAsync(condition, Arg.Any<CancellationToken>())
            .Returns(new OrchestrationStatusQueryResult
            {
                DurableOrchestrationState = durableOrchestrationState
            });

        _durableClient
            .PurgeInstanceHistoryAsync(instanceId2)
            .Returns(new PurgeHistoryResult(1));

        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => _utcNow))
        {
            await _purgeTask.Run(_timer, _durableClient, NullLogger.Instance);
        }

        await _durableClient
            .Received(1)
            .PurgeInstanceHistoryAsync(instanceId2);
    }
}
