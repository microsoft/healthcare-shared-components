// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.DurableTask;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;

namespace Microsoft.Health.Functions.Worker.Examples.Sorting;

public sealed class SortingCheckpoint(int[] values, int sortedLength = 1, DateTimeOffset? createdAtTime = null) : SortingInput(values), IOrchestrationCheckpoint
{
    DateTime? IOperationCheckpoint.CreatedTime => CreatedAtTime?.DateTime;

    public DateTimeOffset? CreatedAtTime { get; } = createdAtTime;

    public int? PercentComplete => Values.Length == 0 ? 100 : (int)((double)SortedLength / Values.Length * 100);

    public IReadOnlyCollection<string>? ResourceIds => null;

    public int SortedLength { get; } = EnsureArg.IsGt(sortedLength, 0, nameof(sortedLength));

    public object? GetResults(string serializedOutput, DataConverter converter)
    {
        if (string.IsNullOrEmpty(serializedOutput))
            return Values;

        return EnsureArg.IsNotNull(converter, nameof(converter)).Deserialize<int[]>(serializedOutput);
    }
}
