// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core;
using Microsoft.Health.Functions.Extensions;

namespace Microsoft.Health.Operations.Functions.Worker.Management;

/// <summary>
/// Represents a periodic process that purges stale orchestration instance metadata from storage.
/// </summary>
public sealed class PurgeOrchestrationInstanceHistory
{
    private readonly PurgeHistoryOptions _options;
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
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        EnsureArg.HasItems(_options.Statuses, nameof(options));
        TimeSpan.FromDays(EnsureArg.IsGt(_options.MinimumAgeDays, 0, nameof(options)));
    }

    /// <summary>
    /// Asynchronously purges the task hub based on the current time.
    /// </summary>
    /// <param name="myTimer">The timer which tracks the invocation schedule.</param>
    /// <param name="client">A client for accessing the task hub.</param>
    /// <param name="log">A diagnostic logger.</param>
    /// <param name="cancellationToken">An optional token for cancellation.</param>
    /// <returns>A task that represents the asynchronous purge operation.</returns>
    [FunctionName(nameof(PurgeOrchestrationInstanceHistory))]
    public async Task Run([TimerTrigger(PurgeFrequencyVariable)] TimerInfo myTimer, [DurableClient] DurableTaskClient client, ILogger log, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(myTimer, nameof(myTimer));
        EnsureArg.IsNotNull(log, nameof(log));

        IReadOnlyCollection<OrchestrationRuntimeStatus> statuses = _options.Statuses!;
        TimeSpan minimumAge = TimeSpan.FromDays(_options.MinimumAgeDays);
        IReadOnlyCollection<string> excludeFunctions = _options.ExcludeFunctions == null ? Array.Empty<string>() : _options.ExcludeFunctions;

        log.LogInformation("Purging orchestration instance history at: {Timestamp}", Clock.UtcNow);
        if (myTimer.IsPastDue)
        {
            log.LogWarning("Current function invocation is running late.");
        }

        DateTimeOffset end = Clock.UtcNow - minimumAge;
        log.LogInformation("Purging all orchestration instances with status in {{{Statuses}}} that started before '{End}' and not in {{{ListofInstanceToSkipPurging}}}.",
            string.Join(", ", statuses),
            end,
            string.Join(", ", excludeFunctions));

        OrchestrationQuery query = new OrchestrationQuery
        {
            CreatedFrom = DateTime.MinValue,
            CreatedTo = end.UtcDateTime,
            Statuses = statuses,
        };

        IAsyncEnumerable<OrchestrationMetadata> instances = client
            .GetAllInstancesAsync(query)
            .Where(x => !excludeFunctions.Contains(x.Name, StringComparer.OrdinalIgnoreCase));

        int purgedInstances = 0;
        await foreach (OrchestrationMetadata instance in instances.WithCancellation(cancellationToken))
        {
            PurgeResult result = await client.PurgeInstanceAsync(instance.InstanceId, cancellationToken);
            log.LogInformation("Instance '{InstanceName}' with {InstanceId} deleted from the task hub.", instance.Name, instance.InstanceId);
            purgedInstances++;
        }

        log.LogInformation("Deleted '{Count}' orchestration instances from the task hub.", purgedInstances);
    }
}
