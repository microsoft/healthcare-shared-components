// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys;
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

        await keyClient.GetKeyAsync(customerManagedKeyOptions.KeyName, customerManagedKeyOptions.KeyVersion, cancellationToken).ConfigureAwait(false);
    }
}
