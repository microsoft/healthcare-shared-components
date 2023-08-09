// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;
using Azure.Security.KeyVault.Keys;
using EnsureThat;
using Microsoft.Health.Core.Features.Identity;
using Microsoft.Health.CustomerManagedKey.Configs;

namespace Microsoft.Health.CustomerManagedKey.Client;

internal static class CustomerKeyClientFactory
{
    /// <summary>
    /// Creates a KeyClient for the KeyVault used to store the customer-managed key
    /// </summary>
    /// <param name="credentialProvider"></param>
    /// <param name="cmkOptions"></param>
    /// <returns></returns>
    public static KeyClient Create(IExternalCredentialProvider credentialProvider, CustomerManagedKeyOptions cmkOptions)
    {
        EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));
        EnsureArg.IsNotNull(cmkOptions, nameof(cmkOptions));

        if (!string.IsNullOrEmpty(cmkOptions.KeyName) && cmkOptions.KeyVaultUri != null)
        {
            TokenCredential externalCredential = credentialProvider.GetTokenCredential();
            return new KeyClient(cmkOptions.KeyVaultUri, externalCredential);
        }

        return null;
    }
}
