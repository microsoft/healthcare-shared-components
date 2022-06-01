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
using Microsoft.Health.Client.Configuration;

namespace Microsoft.Health.Client;

public class ManagedIdentityCredentialProvider : CredentialProvider
{
    private readonly IOptionsMonitor<ManagedIdentityCredentialConfiguration> _managedIdentityCredentialConfigurationMonitor;
    private readonly string _optionsName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityCredentialProvider"/> class.
    /// This class is used to obtain a token for the configured resource via Managed Identity.
    /// </summary>
    /// <param name="managedIdentityCredentialConfigurationMonitorMonitor">The configuration of the token to obtain.</param>
    /// <param name="optionsName">Optional name to use when retrieving options from the IOptionsMonitor</param>
    public ManagedIdentityCredentialProvider(IOptionsMonitor<ManagedIdentityCredentialConfiguration> managedIdentityCredentialConfigurationMonitorMonitor, string optionsName = null)
    {
        EnsureArg.IsNotNull(managedIdentityCredentialConfigurationMonitorMonitor, nameof(managedIdentityCredentialConfigurationMonitorMonitor));

        _managedIdentityCredentialConfigurationMonitor = managedIdentityCredentialConfigurationMonitorMonitor;
        _optionsName = optionsName;
    }

    protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
    {
        ManagedIdentityCredentialConfiguration managedIdentityCredentialConfiguration = _managedIdentityCredentialConfigurationMonitor.Get(_optionsName);

        var defaultAzureCredential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(
            scopes: new[] { managedIdentityCredentialConfiguration.Resource },
            tenantId: managedIdentityCredentialConfiguration.TenantId);

        AccessToken accessToken = await defaultAzureCredential.GetTokenAsync(tokenRequestContext, cancellationToken);
        return accessToken.Token;
    }
}
