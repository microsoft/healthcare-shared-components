// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Health.Blob.Registration;

/// <summary>
/// Represents a builder for configuring Azure Blob Storage container initialization.
/// </summary>
public sealed class BlobContainerInitializationBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContainerInitializationBuilder"/> class
    /// which the specified services.
    /// </summary>
    /// <param name="services">A collection of services.</param>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public BlobContainerInitializationBuilder(IServiceCollection services)
        => Services = EnsureArg.IsNotNull(services, nameof(services));

    /// <summary>
    /// Gets or sets the collection of services being configured.
    /// </summary>
    /// <value>The collection of services, including those necessary for container initialization.</value>
    public IServiceCollection Services { get; }
}
