// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using EnsureThat;
using Microsoft.Health.CustomerManagedKey.Configs;

namespace Microsoft.Health.CustomerManagedKey.Health;

public class KeyWrapUnwrapTestProvider : IKeyTestProvider
{
    public async Task PerformTestAsync(KeyClient keyClient, CustomerManagedKeyOptions customerManagedKeyOptions, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(customerManagedKeyOptions, nameof(customerManagedKeyOptions));

        if (keyClient == null || string.IsNullOrEmpty(customerManagedKeyOptions.KeyName))
        {
            // customer-managed key is not enabled
            return;
        }

        KeyVaultKey key = await keyClient.GetKeyAsync(customerManagedKeyOptions.KeyName, customerManagedKeyOptions.KeyVersion, cancellationToken).ConfigureAwait(false);
        CryptographyClient cryptClient = keyClient.GetCryptographyClient(customerManagedKeyOptions.KeyName, customerManagedKeyOptions.KeyVersion);

        UnwrapResult unwrappedKey = await cryptClient.UnwrapKeyAsync(KeyWrapAlgorithm.Rsa15, key.Key.T, cancellationToken).ConfigureAwait(false);
        await cryptClient.WrapKeyAsync(KeyWrapAlgorithm.Rsa15, unwrappedKey.Key, cancellationToken).ConfigureAwait(false);
    }
}
