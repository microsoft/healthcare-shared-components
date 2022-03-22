// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
    private readonly IReadOnlyCollection<OrchestrationStatus> _statuses;
    private readonly TimeSpan _minimumAge;

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

        DateTimeOffset end = Clock.UtcNow;
        DateTimeOffset start = end - _minimumAge;
        log.LogInformation("Purging all orchestration instances with status in {{{Statuses}}} that started between '{Start}' and '{End}'",
            string.Join(", ", _statuses),
            start,
            end);

        PurgeHistoryResult result = await client.PurgeInstanceHistoryAsync(start.UtcDateTime, end.UtcDateTime, _statuses);

        if (result.InstancesDeleted > 0)
        {
            log.LogInformation("Deleted {Count} orchestration instances from storage.", result.InstancesDeleted);
        }
        else
        {
            log.LogInformation("No Orchestration instances found within given parameters.");
        }
    }
}
