// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Operations.Functions.Worker.DurableTask;

namespace Microsoft.Health.Functions.Worker.Examples.Sorting;

public class SortingOptions
{
    public const string SectionName = "Sorting";

    public bool Ascending { get; set; } = true;

    public ActivityRetryPolicy Retry { get; set; } = new ActivityRetryPolicy();

    public Comparer<int> GetComparer()
        => Ascending ? Comparer<int>.Default : Comparer<int>.Create((x, y) => Comparer<int>.Default.Compare(y, x));
}
