// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using EnsureThat;

namespace Microsoft.Health.Functions.Examples.Sorting;

public class SortingInput
{
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Used as input to orchestrastion.")]
    public int[] Values { get; }

    public SortingInput(int[] values)
        => Values = EnsureArg.IsNotNull(values, nameof(values));
}
