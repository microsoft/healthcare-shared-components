// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Encryption.Customer.Health;

public class KeyAccessAndDataStoreStateDependent : CustomerKeyDependent
{
    public override bool IsImpactedByCustomerKeyHealth(CustomerKeyHealth customerKeyHealth)
    {
        EnsureArg.IsNotNull(customerKeyHealth, nameof(customerKeyHealth));

        return !customerKeyHealth.IsHealthy && (customerKeyHealth.Reason == HealthStatusReason.CustomerManagedKeyAccessLost || customerKeyHealth.Reason == HealthStatusReason.DataStoreStateDegraded);
    }
}
