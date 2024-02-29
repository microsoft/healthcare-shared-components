// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Encryption.Customer.Health;

/// <summary>
/// The health of the encryption key provided by the customer
/// </summary>
public class CustomerKeyHealth
{
    /// <summary>
    /// Gets or sets a boolean representing if the resource is healthy or not
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Gets or sets a reason for the health state
    /// </summary>
    public HealthStatusReason Reason { get; set; } = HealthStatusReason.None;

    /// <summary>
    /// Gets or sets the exception captured from an unhealthy state
    /// </summary>
    public Exception Exception { get; set; }
}
