// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;

namespace Microsoft.Health.Functions.Worker.Examples.Sorting;

public class DistributedSorter(IOptions<SortingOptions> options)
{
    private readonly SortingOptions _options = EnsureArg.IsNotNull(options?.Value, nameof(options));

    [Function(nameof(InsertionSortAsync))]
    public async Task<IReadOnlyList<int>> InsertionSortAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        SortingCheckpoint checkpoint)
    {
        EnsureArg.IsNotNull(context, nameof(context)).ThrowIfInvalidOperationId();
        EnsureArg.IsNotNull(checkpoint, nameof(context));

        ILogger logger = context.CreateReplaySafeLogger<DistributedSorter>();

        if (checkpoint.SortedLength is 1)
            logger.LogInformation("Sorting [{Values}]", string.Join(", ", checkpoint.Values));

        if (checkpoint.SortedLength < checkpoint.Values.Length)
        {
            int sortedLength = checkpoint.SortedLength + 1;
            int[] sorted = await context.CallActivityAsync<int[]>(
                nameof(SortRange),
                checkpoint.Values[0..sortedLength],
                new TaskOptions { Retry = _options.Retry });

            logger.LogInformation(
                "Sorted {SortedLength}/{TotalLength} numbers: [{Values}]",
                sortedLength,
                checkpoint.Values.Length,
                string.Join(", ", checkpoint.Values));

            context.ContinueAsNew(
                new SortingCheckpoint(
                    Concat(sorted, checkpoint.Values[sortedLength..]),
                    sortedLength,
                    checkpoint.CreatedAtTime ?? await context.GetCreatedAtTimeAsync(new TaskOptions { Retry = _options.Retry })));
        }
        else
        {
            logger.LogInformation("Sorting complete: [{Values}]", string.Join(", ", checkpoint.Values));
        }

        return checkpoint.Values;
    }

    [Function(nameof(SortRange))]
    public Task<int[]> SortRange([ActivityTrigger] int[] values)
    {
        EnsureArg.IsNotNull(values, nameof(values));

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
