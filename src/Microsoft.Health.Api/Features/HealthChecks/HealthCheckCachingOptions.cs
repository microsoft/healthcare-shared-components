// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Api.Features.HealthChecks;

/// <summary>
/// Represents a collection of settings related to the caching of <see cref="HealthCheckResult"/> instances.
/// </summary>
public class HealthCheckCachingOptions
{
    /// <summary>
    /// Gets or sets the amount of time for which an instance of <see cref="HealthCheckResult"/> is considered valid.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan Expiry { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the amount of time before the expiry for which the instance of <see cref="HealthCheckResult"/>
    /// should be refreshed.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
    public TimeSpan RefreshOffset { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets a flag that indicates whether <see cref="HealthStatus.Unhealthy"/> should be cached.
    /// </summary>
    public bool CacheFailure { get; set; } = true;
}
