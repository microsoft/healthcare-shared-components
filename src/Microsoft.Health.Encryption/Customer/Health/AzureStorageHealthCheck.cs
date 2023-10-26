// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Core.Features.Health;

namespace Microsoft.Health.Encryption.Customer.Health;

public abstract class AzureStorageHealthCheck : StorageHealthCheck
{
    internal AzureStorageHealthCheck(ValueCache<CustomerKeyHealth> customerKeyHealthCache, ILogger<StorageHealthCheck> logger)
        : base(customerKeyHealthCache, logger)
    {
    }

    public override bool IsCMKAccessLost(Exception ex) => ex is RequestFailedException rfe && rfe.ErrorCode == "KeyVaultEncryptionKeyNotFound";
}
