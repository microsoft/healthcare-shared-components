// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using EnsureThat;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Client.Authentication;

public class ManagedIdentityCredentialProvider : CredentialProvider
{
    private readonly IOptionsMonitor<ManagedIdentityCredentialOptions> _managedIdentityCredentialOptionsMonitor;
    private readonly string _optionsName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityCredentialProvider"/> class.
    /// This class is used to obtain a token for the configured resource via Managed Identity.
    /// </summary>
    /// <param name="managedIdentityCredentialOptionsMonitor">The configuration of the token to obtain.</param>
    /// <param name="optionsName">Optional name to use when retrieving options from the IOptionsMonitor</param>
    public ManagedIdentityCredentialProvider(IOptionsMonitor<ManagedIdentityCredentialOptions> managedIdentityCredentialOptionsMonitor, string optionsName = null)
    {
        EnsureArg.IsNotNull(managedIdentityCredentialOptionsMonitor, nameof(managedIdentityCredentialOptionsMonitor));

        _managedIdentityCredentialOptionsMonitor = managedIdentityCredentialOptionsMonitor;
        _optionsName = optionsName;
    }

    protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
    {
        ManagedIdentityCredentialOptions options = _managedIdentityCredentialOptionsMonitor.Get(_optionsName);
        ManagedIdentityCredential credential = new(options.ClientId);

        TokenRequestContext requestContext = new(scopes: [options.Resource], tenantId: options.TenantId);
        AccessToken accessToken = await credential.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);
        return accessToken.Token;
    }
}
