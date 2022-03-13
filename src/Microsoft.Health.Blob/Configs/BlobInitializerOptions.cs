// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Blob.Configs;

/// <summary>
/// Represents a collection of settings used to configure the initialization of blob containers.
/// </summary>
public class BlobInitializerOptions
{
    /// <summary>
    /// A default configuration section name that may be used for binding.
    /// </summary>
    public const string DefaultSectionName = "Initialization";

    /// <summary>
    /// Gets or sets the delay between initialization retries.
    /// </summary>
    /// <value>The amount of time between retries.</value>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the maximum amount of time to spend initializing containers.
    /// </summary>
    /// <value>The maximum amount of time that initialization may take before an exception is thrown.</value>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(6);
}
