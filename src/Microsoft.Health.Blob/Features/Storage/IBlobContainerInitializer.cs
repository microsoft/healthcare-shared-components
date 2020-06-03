// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Microsoft.Health.Blob.Features.Storage
{
    public interface IBlobContainerInitializer
    {
        Task<BlobContainerClient> InitializeContainerAsync(BlobServiceClient client);
    }
}
