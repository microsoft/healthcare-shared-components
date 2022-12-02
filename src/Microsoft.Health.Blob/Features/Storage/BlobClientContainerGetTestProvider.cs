// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage;

/// <summary>
/// Verifies access to blob container by calling getProperties
/// </summary>
public class BlobClientContainerGetTestProvider : IBlobClientTestProvider
{
    /// <inheritdoc />
    public async Task PerformTestAsync(BlobServiceClient client, BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(client, nameof(client));
        EnsureArg.IsNotNull(blobContainerConfiguration, nameof(blobContainerConfiguration));

        BlobContainerClient containerClient = client.GetBlobContainerClient(blobContainerConfiguration.ContainerName);
        await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
