// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Health.Client.Authentication;

public abstract class CredentialProvider : ICredentialProvider
{
    private readonly TimeSpan _tokenTimeout = TimeSpan.FromMinutes(5);

    internal string Token { get; private set; }

    internal DateTime TokenExpiration { get; private set; }

    public async Task<string> GetBearerToken(CancellationToken cancellationToken)
    {
        if (TokenExpiration < DateTime.UtcNow + _tokenTimeout)
        {
            Token = await BearerTokenFunction(cancellationToken);
            var decodedToken = new JsonWebToken(Token);
            TokenExpiration = decodedToken.ValidTo;
        }

        return Token;
    }

    protected abstract Task<string> BearerTokenFunction(CancellationToken cancellationToken);
}
