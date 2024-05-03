// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using EnsureThat;
using Microsoft.Health.Operations;
using Microsoft.Health.Operations.Functions.DurableTask;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Functions.Examples.Sorting;

internal sealed class SortingCheckpoint(int[] values, int sortedLength = 1, DateTimeOffset? createdAtTime = null) : SortingInput(values), IOrchestrationCheckpoint
{
    DateTime? IOperationCheckpoint.CreatedTime => CreatedAtTime?.DateTime;

    public DateTimeOffset? CreatedAtTime { get; } = createdAtTime;

    public int? PercentComplete => Values.Length == 0 ? 100 : (int)((double)SortedLength / Values.Length * 100);

    public IReadOnlyCollection<string>? ResourceIds => null;

    [DefaultValue(1)]
    [JsonProperty(nameof(SortedLength), DefaultValueHandling = DefaultValueHandling.Populate)]
    public int SortedLength { get; } = EnsureArg.IsGt(sortedLength, 0, nameof(sortedLength));

    public object? GetResults(JToken? output)
        => output?.ToObject<int[]>() ?? Values;
}
