// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Core.Features.Health;

/// <summary>
/// The reason why the service is in its current health state.
/// The values of this enum are ordered from most healthy to least healthy
/// </summary>
public enum HealthStatusReason
{
    /// <summary>
    /// Healthy status reasons, in order of most healthy to least healthy
    /// </summary>
    None,

    /// <summary>
    /// Degraded status reasons, in order of most healthy to least healthy
    /// </summary>
    ServiceDegraded,
    DataStoreConnectionDegraded,
    DataStoreStateDegraded,
    ConnectedStoreDegraded,
    CustomerManagedKeyAccessLost,

    /// <summary>
    /// Unhealthy status reasons, in order of most healthy to least healthy
    /// </summary>
    ServiceUnavailable
}
