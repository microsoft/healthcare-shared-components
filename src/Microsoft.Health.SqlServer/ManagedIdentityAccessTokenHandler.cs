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
    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(string resource, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNullOrEmpty(resource, nameof(resource));

        ManagedIdentityCredential credential = new ManagedIdentityCredential();

        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { resource }), CancellationToken.None).ConfigureAwait(false);

        return token.Token;
    }
}
