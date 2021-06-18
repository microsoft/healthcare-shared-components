// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Checkpoints
{
    public class Checkpoint : ICheckpoint
    {
        public string Identifier { get; set; }

        public DateTimeOffset LastProcessedDt { get; set; }

        public string LastProcessedId { get; set; }
    }
}
