// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Blob.Configs
{
    public class BlobInitializerOptions
    {
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(15);

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(6);
    }
}
