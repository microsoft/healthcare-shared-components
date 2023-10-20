// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Encryption.Customer.Health;

public abstract class CustomerKeyDependent
{
    public abstract bool IsImpactedByCustomerKeyHealth(CustomerKeyHealth customerKeyHealth);
}
