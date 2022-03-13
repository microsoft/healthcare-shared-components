// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

public class CompatibleVersions
{
    public CompatibleVersions(int min, int max)
    {
        EnsureArg.IsLte(min, max);

        Min = min;
        Max = max;
    }

    public int Min { get; }

    public int Max { get; }
}
