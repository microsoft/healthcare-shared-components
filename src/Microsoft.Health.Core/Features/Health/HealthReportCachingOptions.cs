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
    /// to disable expiry.
    /// </remarks>
    /// <value>
    /// Defaults to 5 minutes. This is intentionally several multiples of the typical publisher
    /// <c>Period</c> (30s&#8211;1m) so that a single slow or skipped publish iteration does not
    /// flip the service to <c>503</c>, while still bounding how long a wedged publisher can keep
    /// serving a stale <c>Healthy</c> report. Five minutes also comfortably exceeds the worst-case
    /// duration of a single probe (for example, the ~225s SqlClient retry budget for a transient
    /// login failure), leaving roughly one publish cycle of headroom before the cache is treated
    /// as stale.
    /// </value>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(5);
}
