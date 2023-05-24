// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;
public class AzureTokenCredentialProvider : IAzureTokenCredentialProvider
{
    private readonly TokenCredential _tokenCredential;
    private readonly string _azureResource = "https://database.windows.net/.default";

    public AzureTokenCredentialProvider(
      IOptions<SqlServerDataStoreConfiguration> options)
    {
        string managedIdentityClientId = options?.Value.ManagedIdentityClientId;
        SqlServerAuthenticationType sqlServerAuthenticationType = options.Value.AuthenticationType;

        _tokenCredential = sqlServerAuthenticationType == SqlServerAuthenticationType.ManagedIdentity ?
            new ManagedIdentityCredential(managedIdentityClientId) : new WorkloadIdentityCredential();
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await _tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { _azureResource }), cancellationToken).ConfigureAwait(false);
        return token.Token;
    }
}
