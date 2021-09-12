// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Storage;

namespace Microsoft.Health.Blob.Configs
{
    /// <summary>
    /// Represents a collection of settings used to configure blob operations.
    /// </summary>
    public class BlobOperationOptions
    {
        /// <summary>
        /// Gets or sets the storage transfer settings for downloading blobs.
        /// </summary>
        /// <value>The <see cref="StorageTransferOptions"/> used when reading a blob.</value>
        public StorageTransferOptions Download { get; set; }

        /// <summary>
        /// Gets or sets the storage transfer settings for uploading blobs.
        /// </summary>
        /// <value>The <see cref="StorageTransferOptions"/> used when writing a blob.</value>
        public StorageTransferOptions Upload { get; set; }
    }
}
