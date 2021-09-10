// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage;

namespace Microsoft.Health.Blob.Configs
{
    public class BlobOperationOptions
    {
        public StorageTransferOptions Download { get; set; }

        public StorageTransferOptions Upload { get; set; }
    }
}
