// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Core.Features.Health;

/// <summary>
/// The reason why the service is in its current health state. In order of severity
/// </summary>
public enum HealthStatusReason
{
    None,
    ServiceDegraded,
    CustomerManagedKeyAccessLost,
    ServiceUnavailable
}
