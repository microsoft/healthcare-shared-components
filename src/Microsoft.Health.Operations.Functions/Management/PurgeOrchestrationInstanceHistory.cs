// -------------------------------------------------------------------------------------------------
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core;
using Microsoft.Health.Functions.Extensions;

namespace Microsoft.Health.Operations.Functions.Management;

/// <summary>
/// Represents a periodic process that purges stale orchestration instance metadata from storage.
/// </summary>
public sealed class PurgeOrchestrationInstanceHistory
{
    private readonly IReadOnlyCollection<OrchestrationRuntimeStatus> _statuses;
    private readonly TimeSpan _minimumAge;
    private readonly HashSet<string> _instancesToSkipPurging;

    private const string PurgeFrequencyVariable = $"%{AzureFunctionsJobHost.RootSectionName}:{PurgeHistoryOptions.SectionName}:{nameof(PurgeHistoryOptions.Frequency)}%";

    /// <summary>
    /// Initializes a new instance of the <see cref="PurgeOrchestrationInstanceHistory"/> class based
    /// on the provided options.
    /// </summary>
    /// <param name="options">A collection of options for configuring the purge process.</param>
    /// <exception cref="ArgumentException"><see cref="PurgeHistoryOptions.Statuses"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or its value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="PurgeHistoryOptions.MinimumAgeDays"/> less than or equal to zero.
    /// </exception>
    public PurgeOrchestrationInstanceHistory(IOptions<PurgeHistoryOptions> options)
    {
        PurgeHistoryOptions value = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _statuses = EnsureArg.HasItems(value.Statuses, nameof(options));
        _minimumAge = TimeSpan.FromDays(EnsureArg.IsGt(value.MinimumAgeDays, 0, nameof(options)));
        _instancesToSkipPurging = value.InstancesToSkipPurging == null ? new HashSet<string>() : value.InstancesToSkipPurging.ToHashSet<string>();
    }

    /// <summary>
    /// Asynchronously purges the task hub based on the current time.
    /// </summary>
    /// <param name="myTimer">The timer which tracks the invocation schedule.</param>
    /// <param name="client">A client for accessing the task hub.</param>
    /// <param name="log">A diagnostic logger.</param>
    /// <returns>A task that represents the asynchronous purge operation.</returns>
    [FunctionName(nameof(PurgeOrchestrationInstanceHistory))]
    public async Task Run([TimerTrigger(PurgeFrequencyVariable)] TimerInfo myTimer, [DurableClient] IDurableOrchestrationClient client, ILogger log)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(myTimer, nameof(myTimer));
        EnsureArg.IsNotNull(log, nameof(log));

        log.LogInformation("Purging orchestration instance history at: {Timestamp}", Clock.UtcNow);
        if (myTimer.IsPastDue)
        {
            log.LogWarning("Current function invocation is running late.");
        }

        DateTimeOffset end = Clock.UtcNow - _minimumAge;
        log.LogInformation("Purging all orchestration instances with status in {{{Statuses}}} that started before '{End}' and not in {{{ListofInstanceToSkipPurging}}}.",
            string.Join(", ", _statuses),
            end,
            string.Join(", ", _instancesToSkipPurging));

        OrchestrationStatusQueryCondition condition = new OrchestrationStatusQueryCondition
        {
            CreatedTimeFrom = DateTime.MinValue,
            CreatedTimeTo = end.UtcDateTime,
            RuntimeStatus = _statuses
        };

        var instances = await client.ListInstancesAsync(condition, CancellationToken.None);

        var instancesToPurge = instances.DurableOrchestrationState.Where(x => !_instancesToSkipPurging.Contains(x.InstanceId, StringComparer.OrdinalIgnoreCase));

        foreach(var instance in instancesToPurge)
        {
            var result = await client.PurgeInstanceHistoryAsync(instance.InstanceId);
            log.LogInformation("Instance with {InstanceId} deleted from storage", result.InstancesDeleted);
        }

        if (!instancesToPurge.Any())
        { 
            log.LogInformation("No Orchestration instances found within given parameters.");
        }
    }
}
