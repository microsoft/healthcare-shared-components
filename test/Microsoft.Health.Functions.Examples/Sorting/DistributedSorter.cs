// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

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

        context.ThrowIfInvalidInstanceId();
        logger = context.CreateReplaySafeLogger(logger);

        SortingCheckpoint checkpoint = context.GetInput<SortingCheckpoint>();
        if (checkpoint.Index == 1)
        {
            logger.LogInformation("Sorting [{Values}]", string.Join(", ", checkpoint.Values));
        }

        if (checkpoint.Index < checkpoint.Values.Length)
        {
            int j = await context.CallActivityWithRetryAsync<int>(
                nameof(FindInsertionIndexAsync),
                _options.Retry,
                new SortingInput { Index = checkpoint.Index, Values = checkpoint.Values });

            Swap(checkpoint.Values, checkpoint.Index, j);

            context.ContinueAsNew(
                new SortingCheckpoint
                {
                    CreatedTime = checkpoint.CreatedTime ?? await context.GetCreatedTimeAsync(_options.Retry),
                    Index = checkpoint.Index + 1,
                    Values = checkpoint.Values
                });
        }
        else
        {
            logger.LogInformation("Sorting complete");
        }

        return checkpoint.Values;
    }

    [FunctionName(nameof(FindInsertionIndexAsync))]
    public Task<int> FindInsertionIndexAsync([ActivityTrigger] SortingInput input, ILogger logger)
    {
        EnsureArg.IsNotNull(input, nameof(input));
        EnsureArg.IsNotNull(logger, nameof(logger));

        IComparer<int> comparer = _options.GetComparer();
        int first = input.Values[input.Index];

        int j = input.Index - 1;
        while (j >= 0)
        {
            int second = input.Values[j--];
            if (comparer.Compare(first, second) >= 0)
                break;
        }

        logger.LogInformation("Swapping index {i} with index {j}", input.Index, j);
        return Task.FromResult(j);
    }

    private static void Swap<T>(T[] values, int i, int j)
    {
        T tmp = values[i];
        values[i] = values[j];
        values[j] = tmp;
    }
}
