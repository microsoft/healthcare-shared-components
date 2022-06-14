// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.Client.Authentication;

public class AuthenticationHttpMessageHandler : DelegatingHandler
{
    private readonly ICredentialProvider _credentialProvider;

    public AuthenticationHttpMessageHandler(ICredentialProvider credentialProvider)
    {
        EnsureArg.IsNotNull(credentialProvider, nameof(credentialProvider));

        _credentialProvider = credentialProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await _credentialProvider.GetBearerToken(cancellationToken).ConfigureAwait(false));

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
