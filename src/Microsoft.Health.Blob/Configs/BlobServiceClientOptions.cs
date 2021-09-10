// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage.Blobs;

namespace Microsoft.Health.Blob.Configs
{
    public class BlobServiceClientOptions : BlobClientOptions
    {
        public string ConnectionString { get; set; }

        public string Credential { get; set; }

        public string ClientId { get; set; }

        public BlobOperationOptions Operations { get; set; }
    }
}
