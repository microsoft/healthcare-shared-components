// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask.Core;
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
    private readonly PurgeOrchestrationInstanceHistory _purgeTask;
    private readonly IDurableOrchestrationClient _durableClient;

    public PurgeOrchestrationInstanceHistoryTests()
    {
        _utcNow = DateTime.UtcNow;
        _timer = Substitute.For<TimerInfo>(default, default, default);
        _purgeConfig = new PurgeHistoryOptions
        {
            Statuses = new HashSet<OrchestrationStatus> { OrchestrationStatus.Completed },
            MinimumAgeDays = 14,
        };
        _purgeTask = new PurgeOrchestrationInstanceHistory(Options.Create(_purgeConfig));
        _durableClient = Substitute.For<IDurableOrchestrationClient>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public async Task GivenNoOrchestrationInstances_WhenPurgeCompletedDurableFunctionsHistory_ThenNoOrchestrationsPurgedAsync(int deleted)
    {
        _durableClient
            .PurgeInstanceHistoryAsync(
                DateTime.MinValue,
                _utcNow.AddDays(-_purgeConfig.MinimumAgeDays),
                _purgeConfig.Statuses)
            .Returns(new PurgeHistoryResult(deleted));

        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => _utcNow))
        {
            await _purgeTask.Run(_timer, _durableClient, NullLogger.Instance);
        }

        await _durableClient
            .Received(1)
            .PurgeInstanceHistoryAsync(
                DateTime.MinValue,
                _utcNow.AddDays(-_purgeConfig.MinimumAgeDays),
                _purgeConfig.Statuses);
    }
}
