// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.Encryption.Customer.Configs;

public class CustomerManagedKeyOptions
{
    public const string CustomerManagedKey = "CustomerManagedKey";

    public Uri KeyVaultUri { get; set; }

    public string KeyVersion { get; set; }

    public string KeyName { get; set; }

    public int KeyValidationPeriodInSeconds { get; set; }
}
