// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Operations.Functions.DurableTask;

namespace Microsoft.Health.Functions.Examples.Sorting;

public class SortingOptions
{
    public const string SectionName = "Sorting";

    public bool Ascending { get; set; } = true;

    public ActivityRetryOptions Retry { get; set; } = new ActivityRetryOptions();

    public IComparer<int> GetComparer()
        => Ascending ? Comparer<int>.Default : Comparer<int>.Create((x, y) => Comparer<int>.Default.Compare(y, x));
}
