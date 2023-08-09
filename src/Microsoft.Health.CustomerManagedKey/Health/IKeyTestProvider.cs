// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Keys;
using Microsoft.Health.CustomerManagedKey.Configs;

namespace Microsoft.Health.CustomerManagedKey.Health;

public interface IKeyTestProvider
{
    Task PerformTestAsync(KeyClient keyClient, CustomerManagedKeyOptions customerManagedKeyOptions, CancellationToken cancellationToken);
}
