// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.Blob.Configs;

namespace Microsoft.Health.Blob.Features.Storage;

/// <summary>
/// Provides methods for creating a BlobServiceClient instance and initializing containers.
/// </summary>
public interface IBlobInitializer
{
    /// <summary>
    /// Open blobservice client.
    /// </summary>
    /// <param name="blobContainerConfiguration">The container configuration to use for validating the blob client is open.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    Task OpenBlobClientAsync(BlobContainerConfiguration blobContainerConfiguration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize data store.
    /// </summary>
    /// <param name="containerInitializers">The blob container initializers.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    Task InitializeDataStoreAsync(IEnumerable<IBlobContainerInitializer> containerInitializers, CancellationToken cancellationToken = default);
}
