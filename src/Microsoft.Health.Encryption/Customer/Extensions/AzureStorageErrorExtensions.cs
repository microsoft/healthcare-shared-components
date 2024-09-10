// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Azure;

namespace Microsoft.Health.Encryption.Customer.Extensions;

public static class AzureStorageErrorExtensions
{
    private const string KeyVaultEncryptionNotFoundErrorCode = "KeyVaultEncryptionKeyNotFound";
    private const string KeyVaultNotFoundErrorCode = "KeyVaultVaultNotFound";

    private static readonly List<string> KeyVaultErrorCodes = new List<string>() { KeyVaultEncryptionNotFoundErrorCode, KeyVaultNotFoundErrorCode };

    public static bool IsCMKError(this RequestFailedException rfe)
    {
        return KeyVaultErrorCodes.Exists(value => value.Equals(rfe?.ErrorCode, StringComparison.OrdinalIgnoreCase));
    }
}
