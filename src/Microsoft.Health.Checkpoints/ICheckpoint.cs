// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Checkpoints
{
    public interface ICheckpoint
    {
        string Partition { get; set; }

        string Identifier { get; set; }

        Azure.ETag ETag { get; set; }

        DateTimeOffset LastProcessedDateTime { get; set; }

        string LastProcessedIdentifier { get; set; }
    }
}
