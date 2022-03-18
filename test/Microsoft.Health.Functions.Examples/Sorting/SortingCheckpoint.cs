// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Health.Operations;

namespace Microsoft.Health.Functions.Examples.Sorting;

internal class SortingCheckpoint : SortingInput, IOperationCheckpoint
{
    public DateTime? CreatedTime { get; set; }

    public int PercentComplete => Values.Length == 0 ? 100 : (int)((double)Index / Values.Length * 100);

    public IReadOnlyCollection<string>? ResourceIds => null;
}
