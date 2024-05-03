// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Operations.Functions.Worker.Management;

/// <summary>
/// Represents a periodic process that purges stale orchestration instance metadata from storage.
/// </summary>
public sealed class PurgeOrchestrationInstanceHistory
{
    private const string PurgeFrequencyVariable = $"%{AzureFunctionsJobHost.RootSectionName}:{PurgeHistoryOptions.SectionName}:Frequency%";

    private readonly PurgeHistoryOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurgeOrchestrationInstanceHistory"/> class based
    /// on the provided options.
    /// </summary>
    /// <param name="options">A collection of options for configuring the purge process.</param>
    /// <param name="timeProvider">A provider for the current time.</param>
    /// <exception cref="ArgumentException"><see cref="PurgeHistoryOptions.Statuses"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or its value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="PurgeHistoryOptions.MinimumAgeDays"/> less than or equal to zero.
    /// </exception>
    public PurgeOrchestrationInstanceHistory(IOptions<PurgeHistoryOptions> options, TimeProvider timeProvider)
    {
        _options = EnsureArg.IsNotNull(options?.Value, nameof(options));
        _timeProvider = EnsureArg.IsNotNull(timeProvider, nameof(timeProvider));

        EnsureArg.HasItems(_options.Statuses, nameof(options));
        TimeSpan.FromDays(EnsureArg.IsGt(_options.MinimumAgeDays, 0, nameof(options)));
    }

    /// <summary>
    /// Asynchronously purges the task hub based on the current time.
    /// </summary>
    /// <param name="myTimer">The timer which tracks the invocation schedule.</param>
    /// <param name="client">A client for accessing the task hub.</param>
    /// <param name="context">Context for the function's execution.</param>
    /// <returns>A task that represents the asynchronous purge operation.</returns>
    [Function(nameof(PurgeOrchestrationInstanceHistory))]
    public async Task Run([TimerTrigger(PurgeFrequencyVariable)] TimerInfo myTimer, [DurableClient] DurableTaskClient client, FunctionContext context)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(myTimer, nameof(myTimer));
        EnsureArg.IsNotNull(context, nameof(context));

        IReadOnlyCollection<OrchestrationRuntimeStatus> statuses = _options.Statuses!;
        IReadOnlyCollection<string> excludeFunctions = _options.ExcludeFunctions ?? Array.Empty<string>();
        TimeSpan minimumAge = TimeSpan.FromDays(_options.MinimumAgeDays);

        ILogger logger = context.GetLogger<PurgeOrchestrationInstanceHistory>();
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        logger.LogInformation("Purging orchestration instance history at: {Timestamp}", utcNow);
        if (myTimer.IsPastDue)
            logger.LogWarning("Current function invocation is running late.");

        DateTimeOffset end = utcNow - minimumAge;
        logger.LogInformation("Purging all orchestration instances with status in {{{Statuses}}} that started before '{End}' and not in {{{ListofInstanceToSkipPurging}}}.",
            string.Join(", ", statuses),
            end,
            string.Join(", ", excludeFunctions));

        OrchestrationQuery query = new()
        {
            CreatedFrom = DateTime.MinValue,
            CreatedTo = end.UtcDateTime,
            Statuses = statuses,
        };

        IAsyncEnumerable<OrchestrationMetadata> instances = client
            .GetAllInstancesAsync(query)
            .Where(x => !excludeFunctions.Contains(x.Name, StringComparer.OrdinalIgnoreCase));

        int purgedInstances = 0;
        PurgeInstanceOptions options = new() { Recursive = true };
        await foreach (OrchestrationMetadata instance in instances.WithCancellation(context.CancellationToken))
        {
            PurgeResult result = await client.PurgeInstanceAsync(instance.InstanceId, options, context.CancellationToken);
            logger.LogInformation(
                "Instance '{InstanceName}' with {InstanceId} deleted from the task hub and recursively included {Count} instance(s).",
                instance.Name,
                instance.InstanceId,
                result.PurgedInstanceCount);

            purgedInstances += result.PurgedInstanceCount;
        }

        logger.LogInformation("Deleted '{Count}' orchestration instances from the task hub.", purgedInstances);
    }
}
