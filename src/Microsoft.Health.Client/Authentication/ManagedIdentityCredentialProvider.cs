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
        ManagedIdentityCredentialOptions managedIdentityCredentialOptions = _managedIdentityCredentialOptionsMonitor.Get(_optionsName);

        var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions();

        if (!string.IsNullOrEmpty(managedIdentityCredentialOptions.ClientId))
        {
            defaultAzureCredentialOptions.ManagedIdentityClientId = managedIdentityCredentialOptions.ClientId;
        }

        var defaultAzureCredential = new DefaultAzureCredential(defaultAzureCredentialOptions);

        var tokenRequestContext = new TokenRequestContext(
            scopes: new[] { managedIdentityCredentialOptions.Resource },
            tenantId: managedIdentityCredentialOptions.TenantId);

        AccessToken accessToken = await defaultAzureCredential.GetTokenAsync(tokenRequestContext, cancellationToken).ConfigureAwait(false);
        return accessToken.Token;
    }
}
