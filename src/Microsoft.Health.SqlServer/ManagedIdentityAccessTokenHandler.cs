// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using EnsureThat;

namespace Microsoft.Health.SqlServer;

public class ManagedIdentityAccessTokenHandler : IAccessTokenHandler
{
    private readonly string _managedIdentityClientId;

    public ManagedIdentityAccessTokenHandler(string managedIdentityClientId)
    {
        EnsureArg.IsNotNull(managedIdentityClientId, nameof(managedIdentityClientId));

        _managedIdentityClientId = managedIdentityClientId;
    }

    public async Task<string> GetAccessTokenAsync(string resource, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(resource, nameof(resource));

        ManagedIdentityCredential credential = new ManagedIdentityCredential(_managedIdentityClientId);

        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { resource }), CancellationToken.None).ConfigureAwait(false);

        return token.Token;
    }
}
