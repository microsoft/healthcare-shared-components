// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Encryption.Customer.Configs;

namespace Microsoft.Health.Encryption.Customer.Health;

internal class KeyWrapUnwrapTestProvider : IKeyTestProvider
{
    private readonly KeyClient _keyClient;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;

    public KeyWrapUnwrapTestProvider(
        IExternalCredentialProvider credentialProvider,
        IOptions<CustomerManagedKeyOptions> cmkOptions)
    {
        EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
        EnsureArg.IsNotNull(cmkOptions, nameof(cmkOptions));

        _customerManagedKeyOptions = EnsureArg.IsNotNull(cmkOptions?.Value, nameof(cmkOptions));

        if (!string.IsNullOrEmpty(_customerManagedKeyOptions.KeyName) && _customerManagedKeyOptions.KeyVaultUri != null)
        {
            TokenCredential externalCredential = credentialProvider.GetTokenCredential();
            _keyClient = new KeyClient(_customerManagedKeyOptions.KeyVaultUri, externalCredential);
        }
    }

    public async Task PerformTestAsync(CancellationToken cancellationToken = default)
    {
        if (_keyClient == null)
            // customer-managed key is not enabled
            return;

        // Get Key
        await _keyClient.GetKeyAsync(_customerManagedKeyOptions.KeyName, _customerManagedKeyOptions.KeyVersion, cancellationToken).ConfigureAwait(false);

        // Create key for encryption
        byte[] encryptionKey = RandomNumberGenerator.GetBytes(32);

        // Wrap and Unwrap customer key
        CryptographyClient cryptClient = _keyClient.GetCryptographyClient(_customerManagedKeyOptions.KeyName, _customerManagedKeyOptions.KeyVersion);
        WrapResult wrappedKey = await cryptClient.WrapKeyAsync(KeyWrapAlgorithm.Rsa15, encryptionKey, cancellationToken).ConfigureAwait(false);
        await cryptClient.UnwrapKeyAsync(wrappedKey.Algorithm, wrappedKey.EncryptedKey, cancellationToken).ConfigureAwait(false);
    }
}
