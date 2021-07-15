// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Blob.Configs
{
    public class BlobDataStoreConfiguration
    {
        public const string SectionName = "BlobStore";

        public string ConnectionString { get; set; }

        public BlobDataStoreAuthenticationType AuthenticationType { get; set; } = BlobDataStoreAuthenticationType.ConnectionString;

        public BlobDataStoreRequestOptions RequestOptions { get; } = new BlobDataStoreRequestOptions();

        /// <summary>
        /// If set, the app ID of the user assigned managed identity to use when connecting to azure storage, if AUthenticationType == ManagedIdentity.
        /// </summary>
        public string UserAssignedManagedIdentityAppId { get; set; }
    }
}
