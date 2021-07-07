// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure.Storage.Blobs;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Health.Blob.Registration
{
    /// <summary>
    /// Represents a builder for configuring <see cref="BlobServiceClient"/> instances.
    /// </summary>
    public sealed class BlobClientBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobClientBuilder"/> class with the specified services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for the client being configured.</param>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
        public BlobClientBuilder(IServiceCollection services)
            => Services = EnsureArg.IsNotNull(services, nameof(services));

        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> for the client being configured.
        /// </summary>
        /// <value>A service collection containing the configured <see cref="BlobServiceClient"/>.</value>
        public IServiceCollection Services { get; }
    }
}
