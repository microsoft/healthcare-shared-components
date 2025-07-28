// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Encryption.Customer.Health;

internal static class CustomerManagedKeyHealthCache
{
    public static ValueCache<CustomerKeyHealth> Instance { get; } = new();
}
