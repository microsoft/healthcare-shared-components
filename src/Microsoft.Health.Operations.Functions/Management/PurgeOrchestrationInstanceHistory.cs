// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core;

namespace Microsoft.Health.Operations.Functions;

/// <summary>
/// Represents a periodic process that purges stale orchestration instance metadata from storage.
/// </summary>
public sealed class PurgeOrchestrationInstanceHistory
{
    private readonly PurgeHistoryOptions _options;

    private const string PurgeFrequencyVariable = $"%{AzureFunctionsJobHost.SectionName}:{PurgeHistoryOptions.SectionName}:{nameof(PurgeHistoryOptions.Frequency)}%";

    /// <summary>
    /// Initializes a new instance of the <see cref="PurgeOrchestrationInstanceHistory"/> class based
    /// on the provided options.
    /// </summary>
    /// <param name="options">A collection of options for configuring the purge process.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or its value is <see langword="null"/>.</exception>
    public PurgeOrchestrationInstanceHistory(IOptions<PurgeHistoryOptions> options)
        => _options = EnsureArg.IsNotNull(options?.Value, nameof(options));

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

        (DateTime start, DateTime end) = (DateTime.MinValue, Clock.UtcNow.UtcDateTime.AddDays(-_options.MinimumAgeDays));
        log.LogInformation("Purging all orchestration instances with status in {{{Statuses}}} that started between '{Start}' and '{End}'",
            string.Join(", ", _options.Statuses!),
            start,
            end);

        PurgeHistoryResult result = await client.PurgeInstanceHistoryAsync(start, end, _options.Statuses);

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
