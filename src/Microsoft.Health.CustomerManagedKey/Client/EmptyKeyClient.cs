// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Security.KeyVault.Keys;

namespace Microsoft.Health.CustomerManagedKey.Client;

internal class EmptyKeyClient : KeyClient
{
    public EmptyKeyClient() { }
}
