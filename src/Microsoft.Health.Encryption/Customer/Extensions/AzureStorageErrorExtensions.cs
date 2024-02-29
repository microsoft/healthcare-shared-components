// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure;

namespace Microsoft.Health.Encryption.Customer.Extensions;

public static class AzureStorageErrorExtensions
{
    private const string KeyVaultEncryptionNotFoundErrorCode = "KeyVaultEncryptionKeyNotFound";

    public static bool IsCMKError(this RequestFailedException rfe)
    {
        return rfe?.ErrorCode == KeyVaultEncryptionNotFoundErrorCode;
    }
}
