// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage
{
    public interface IBlobClientTestProvider
    {
        Task PerformTestAsync(BlobServiceClient client, BlobDataStoreConfiguration configuration, BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default);
    }
}
