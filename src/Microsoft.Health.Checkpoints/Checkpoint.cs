// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Checkpoints
{
    public class Checkpoint : ICheckpoint
    {
        public string Partition { get; set; }

        public string Identifier { get; set; }

        public string ETag { get; set; }

        public DateTimeOffset LastProcessedDateTime { get; set; }

        public string LastProcessedIdentifier { get; set; }
    }
}
