// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Checkpoints
{
    public interface ICheckpoint
    {
        string Identifier { get; set; }

        DateTimeOffset LastProcessedDt { get; set; }

        string LastProcessedId { get; set; }
    }
}
