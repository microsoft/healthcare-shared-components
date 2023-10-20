// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.Features.Health;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.Encryption.Customer.Configs;

namespace Microsoft.Health.Encryption.Customer.Health;

internal class KeyWrapUnwrapTestProvider : ICustomerKeyTestProvider
{
    private const string AccessLostMessage = "Access to the customer-managed key has been lost";

    private readonly KeyClient _keyClient;
    private readonly CustomerManagedKeyOptions _customerManagedKeyOptions;
    private readonly ILogger<KeyWrapUnwrapTestProvider> _logger;

    public KeyWrapUnwrapTestProvider(
        IExternalCredentialProvider credentialProvider,
        IOptions<CustomerManagedKeyOptions> cmkOptions,
        ILogger<KeyWrapUnwrapTestProvider> logger)
    {
        EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
        EnsureArg.IsNotNull(cmkOptions, nameof(cmkOptions));

        _customerManagedKeyOptions = EnsureArg.IsNotNull(cmkOptions.Value, nameof(cmkOptions.Value));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        if (!string.IsNullOrEmpty(_customerManagedKeyOptions.KeyName) && _customerManagedKeyOptions.KeyVaultUri != null)
        {
            TokenCredential externalCredential = credentialProvider.GetTokenCredential();
            _keyClient = new KeyClient(_customerManagedKeyOptions.KeyVaultUri, externalCredential);
        }
    }

    public int Priority => 1;

    public HealthStatusReason FailureReason => HealthStatusReason.CustomerManagedKeyAccessLost;

    public async Task<CustomerKeyHealth> AssertHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_keyClient == null)
            // customer-managed key is not enabled
            return new CustomerKeyHealth();

        try
        {
            // Get Key
            await _keyClient.GetKeyAsync(_customerManagedKeyOptions.KeyName, _customerManagedKeyOptions.KeyVersion, cancellationToken).ConfigureAwait(false);

            // Create key for encryption
            byte[] encryptionKey = RandomNumberGenerator.GetBytes(32);

            // Wrap and Unwrap customer key
            CryptographyClient cryptClient = _keyClient.GetCryptographyClient(_customerManagedKeyOptions.KeyName, _customerManagedKeyOptions.KeyVersion);
            WrapResult wrappedKey = await cryptClient.WrapKeyAsync(KeyWrapAlgorithm.Rsa15, encryptionKey, cancellationToken).ConfigureAwait(false);
            await cryptClient.UnwrapKeyAsync(wrappedKey.Algorithm, wrappedKey.EncryptedKey, cancellationToken).ConfigureAwait(false);

            return new CustomerKeyHealth();
        }
        catch (Exception ex) when (ex is RequestFailedException or CryptographicException or InvalidOperationException or NotSupportedException)
        {
            _logger.LogInformation(ex, AccessLostMessage);

            return new CustomerKeyHealth
            {
                IsHealthy = false,
                Reason = FailureReason,
                Exception = ex,
            };
        }
    }
}
