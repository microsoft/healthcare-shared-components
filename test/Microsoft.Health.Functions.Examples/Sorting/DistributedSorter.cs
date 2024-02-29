// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Functions.Examples.Sorting;

public class DistributedSorter
{
    private readonly SortingOptions _options;

    public DistributedSorter(IOptions<SortingOptions> options)
        => _options = EnsureArg.IsNotNull(options?.Value, nameof(options));

    [FunctionName(nameof(InsertionSortAsync))]
    public async Task<IReadOnlyList<int>> InsertionSortAsync([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger logger)
    {
        EnsureArg.IsNotNull(context, nameof(context));
        EnsureArg.IsNotNull(logger, nameof(logger));

        context.ThrowIfInvalidOperationId();
        logger = context.CreateReplaySafeLogger(logger);

        SortingCheckpoint checkpoint = context.GetInput<SortingCheckpoint>();
        EnsureArg.IsNotNull(checkpoint, nameof(context));

        if (checkpoint.SortedLength == 1)
        {
            logger.LogInformation("Sorting [{Values}]", string.Join(", ", checkpoint.Values));
        }

        if (checkpoint.SortedLength < checkpoint.Values.Length)
        {
            int sortedLength = checkpoint.SortedLength + 1;
            int[] sorted = await context.CallActivityWithRetryAsync<int[]>(
                nameof(SortRange),
                _options.Retry,
                checkpoint.Values[0..sortedLength]);

            logger.LogInformation(
                "Sorted {SortedLength}/{TotalLength} numbers: [{Values}]",
                sortedLength,
                checkpoint.Values.Length,
                string.Join(", ", checkpoint.Values));

            context.ContinueAsNew(
                new SortingCheckpoint(
                    Concat(sorted, checkpoint.Values[sortedLength..]),
                    sortedLength,
                    checkpoint.CreatedTime ?? await context.GetCreatedTimeAsync(_options.Retry)));
        }
        else
        {
            logger.LogInformation("Sorting complete: [{Values}]", string.Join(", ", checkpoint.Values));
        }

        return checkpoint.Values;
    }

    [FunctionName(nameof(SortRange))]
    public Task<int[]> SortRange([ActivityTrigger] int[] values, ILogger logger)
    {
        EnsureArg.IsNotNull(values, nameof(values));
        EnsureArg.IsNotNull(logger, nameof(logger));

        Comparer<int> comparer = _options.GetComparer();

        for (int j = values.Length - 1; j > 0 && comparer.Compare(values[j - 1], values[j]) > 0; j--)
        {
            (values[j - 1], values[j]) = (values[j], values[j - 1]);
        }

        return Task.FromResult(values);
    }

    private static T[] Concat<T>(T[] left, T[] right)
    {
        var result = new T[left.Length + right.Length];
        Array.Copy(left, result, left.Length);
        Array.Copy(right, 0, result, left.Length, right.Length);

        return result;
    }
}
