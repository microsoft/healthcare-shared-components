// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;

namespace Microsoft.Health.Core.Features.Health;

/// <summary>
/// Options that control how the cached <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport"/>
/// published by <c>HealthCheckCachePublisher</c> is treated by readers (e.g. the cached health-check middleware).
/// </summary>
public class HealthReportCachingOptions
{
    /// <summary>
    /// Gets or sets the maximum age of a published <c>HealthReport</c> before it is considered stale.
    /// </summary>
    /// <remarks>
    /// When the most recent publish is older than this value, readers receive <see langword="null"/>
    /// instead of the stale report and the cached health-check middleware responds with
    /// <c>503 Service Unavailable</c>. This protects against the case where the publisher iteration
    /// has stopped completing (for example, a health check whose underlying dependency is hanging or
    /// throwing for longer than the publisher period). Set to <see cref="Timeout.InfiniteTimeSpan"/>
    /// to disable expiry. Defaults to 5 minutes.
    /// </remarks>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(5);
}
