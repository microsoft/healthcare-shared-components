// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Schema.Manager.Model;

public class MutuallyExclusiveType
{
    public int Version { get; set; }

    public bool Next { get; set; }

    public bool Latest { get; set; }
}
