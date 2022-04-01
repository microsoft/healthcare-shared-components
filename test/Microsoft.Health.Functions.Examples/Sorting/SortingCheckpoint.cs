// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using EnsureThat;
using Microsoft.Health.Operations;
using Newtonsoft.Json;

namespace Microsoft.Health.Functions.Examples.Sorting;

internal class SortingCheckpoint : SortingInput, IOperationCheckpoint
{
    public DateTime? CreatedTime { get; }

    public int? PercentComplete => Values.Length == 0 ? 100 : (int)((double)SortedLength / Values.Length * 100);

    public IReadOnlyCollection<string>? ResourceIds => null;

    [DefaultValue(1)]
    [JsonProperty(nameof(SortedLength), DefaultValueHandling = DefaultValueHandling.Populate)]
    public int SortedLength { get; }

    public IReadOnlyDictionary<string, string>? AdditionalProperties => null;

    public SortingCheckpoint(int[] values, int sortedLength = 1, DateTime? createdTime = null)
        : base(values)
    {
        CreatedTime = createdTime;
        SortedLength = EnsureArg.IsGt(sortedLength, 0, nameof(sortedLength));
    }
}
