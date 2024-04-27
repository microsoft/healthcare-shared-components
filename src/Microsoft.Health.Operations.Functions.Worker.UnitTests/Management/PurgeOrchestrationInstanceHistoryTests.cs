// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Health.Operations.Functions.Worker.Management;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Operations.Functions.Worker.UnitTests.Management;

public class PurgeOrchestrationInstanceHistoryTests
{
    private readonly FakeTimeProvider _timeProvider = new(UtcNow);
    private readonly TimerInfo _timer;
    private readonly PurgeHistoryOptions _purgeConfig;
    private readonly DurableTaskClient _durableClient;
    private readonly FunctionContext _context;
    private readonly PurgeOrchestrationInstanceHistory _purgeTask;

    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;

    public PurgeOrchestrationInstanceHistoryTests()
    {
        _timer = Substitute.For<TimerInfo>();
        _purgeConfig = new PurgeHistoryOptions
        {
            Statuses = [OrchestrationRuntimeStatus.Completed],
            MinimumAgeDays = 14,
        };
        _durableClient = Substitute.For<DurableTaskClient>("TestTaskHub");
        _purgeTask = new PurgeOrchestrationInstanceHistory(Options.Create(_purgeConfig), _timeProvider);
        _context = Substitute.For<FunctionContext>();
        _context
            .InstanceServices
            .Returns(new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public async Task GivenOrchestrationInstances_WhenPurgeCompletedDurableFunctionsHistory_ThenOrchestrationsPurgedAsync(int count)
    {
        List<OrchestrationMetadata> instances = Enumerable
            .Repeat(1, count)
            .Select(x => new OrchestrationMetadata(x.ToString(CultureInfo.InvariantCulture), OperationId.Generate()))
            .ToList();

        _durableClient
            .GetAllInstancesAsync(default)
            .ReturnsForAnyArgs(CreatePaginatedResults(instances, 2));

        _durableClient
            .PurgeInstanceAsync(default!, default, default)
            .ReturnsForAnyArgs(cxt => new PurgeResult(1));

        await _purgeTask.Run(_timer, _durableClient, _context);

        _durableClient
            .Received(1)
            .GetAllInstancesAsync(Arg.Is<OrchestrationQuery?>(x => IsExpectedQuery(x!)));

        foreach (string id in instances.Select(x => x.InstanceId))
            await _durableClient.Received(1).PurgeInstanceAsync(id, Arg.Is<PurgeInstanceOptions>(x => x.Recursive), _context.CancellationToken);
    }

    [Fact]
    public async Task GivenOrchestrationInstancesAndInstancesToSkipPurging_WhenPurgeCompletedDurableFunctionsHistory_ThenNoOrchestrationsPurgedAsync()
    {
        var instanceId1 = OperationId.Generate();
        var instanceId2 = OperationId.Generate();
        var instanceName1 = "Re-Index";
        var instanceName2 = "CopyInstance";

        _purgeConfig.ExcludeFunctions = [instanceName2];

        List<OrchestrationMetadata> instances =
        [
            new OrchestrationMetadata(instanceName1, instanceId1),
            new OrchestrationMetadata(instanceName2, instanceId2),
        ];

        _durableClient
            .GetAllInstancesAsync(default)
            .ReturnsForAnyArgs(CreatePaginatedResults(instances, 1));

        _durableClient
            .PurgeInstanceAsync(default!, default, default)
            .ReturnsForAnyArgs(cxt => new PurgeResult(1));

        await _purgeTask.Run(_timer, _durableClient, _context);

        _durableClient
            .Received(1)
            .GetAllInstancesAsync(Arg.Is<OrchestrationQuery?>(x => IsExpectedQuery(x!)));

        await _durableClient
            .Received(1)
            .PurgeInstanceAsync(instanceId1, Arg.Is<PurgeInstanceOptions>(x => x.Recursive), _context.CancellationToken);

        await _durableClient
            .DidNotReceive()
            .PurgeInstanceAsync(instanceId2, Arg.Any<PurgeInstanceOptions>(), Arg.Any<CancellationToken>());
    }

    private static AsyncPageable<T> CreatePaginatedResults<T>(IList<T> data, int pageSize)
        where T : notnull
    {
        return Pageable.Create((string? continuation, CancellationToken token) =>
        {
            if (string.IsNullOrEmpty(continuation))
                return Task.FromResult(new Page<T>(data.Take(pageSize).ToList(), pageSize.ToString(CultureInfo.InvariantCulture)));

            int skip = int.Parse(continuation, CultureInfo.InvariantCulture);
            return skip >= data.Count
                ? Task.FromResult(new Page<T>([], null))
                : Task.FromResult(new Page<T>(data.Skip(skip).Take(pageSize).ToList(), (skip + pageSize).ToString(CultureInfo.InvariantCulture)));
        });
    }

    private bool IsExpectedQuery(OrchestrationQuery query)
    {
        return query.Statuses!.SequenceEqual(_purgeConfig.Statuses!)
            && query.CreatedFrom == DateTime.MinValue
            && query.CreatedTo == UtcNow.AddDays(-_purgeConfig.MinimumAgeDays);
    }
}
