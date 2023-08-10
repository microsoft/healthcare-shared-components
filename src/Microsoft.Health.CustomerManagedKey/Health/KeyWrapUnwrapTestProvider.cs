// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using EnsureThat;
using Microsoft.Health.CustomerManagedKey.Client;
using Microsoft.Health.CustomerManagedKey.Configs;

namespace Microsoft.Health.CustomerManagedKey.Health;

public class KeyWrapUnwrapTestProvider : IKeyTestProvider
{
    public async Task PerformTestAsync(KeyClient keyClient, CustomerManagedKeyOptions customerManagedKeyOptions, CancellationToken cancellationToken = default)
    {
        EnsureArg.IsNotNull(keyClient, nameof(keyClient));
        EnsureArg.IsNotNull(customerManagedKeyOptions, nameof(customerManagedKeyOptions));

        if (keyClient is EmptyKeyClient || string.IsNullOrEmpty(customerManagedKeyOptions.KeyName))
        {
            // customer-managed key is not enabled
            return;
        }

        // Get Key
        await keyClient.GetKeyAsync(customerManagedKeyOptions.KeyName, customerManagedKeyOptions.KeyVersion, cancellationToken).ConfigureAwait(false);

        // Create key for encryption
        byte[] encryptionKey = RandomNumberGenerator.GetBytes(32);

        // Wrap and Unwrap customer key
        CryptographyClient cryptClient = keyClient.GetCryptographyClient(customerManagedKeyOptions.KeyName, customerManagedKeyOptions.KeyVersion);
        WrapResult wrappedKey = await cryptClient.WrapKeyAsync(KeyWrapAlgorithm.Rsa15, encryptionKey, cancellationToken).ConfigureAwait(false);
        await cryptClient.UnwrapKeyAsync(wrappedKey.Algorithm, wrappedKey.EncryptedKey, cancellationToken).ConfigureAwait(false);
    }
}
