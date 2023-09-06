// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Azure.Identity;
using Microsoft.Health.SqlServer.Configs;
using Azure.Core;

namespace Microsoft.Health.SqlServer;

public class ManagedIdentityAccessTokenHandler : IAccessTokenHandler
{
    private readonly DefaultAzureCredential _azureServiceTokenProvider;

    public SqlServerAuthenticationType AuthenticationType => SqlServerAuthenticationType.ManagedIdentity;

    public string AzureScope => "https://database.windows.net/.default";

    public ManagedIdentityAccessTokenHandler(DefaultAzureCredential azureServiceTokenProvider)
    {
        EnsureArg.IsNotNull(azureServiceTokenProvider, nameof(azureServiceTokenProvider));

        _azureServiceTokenProvider = azureServiceTokenProvider;
    }

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        AccessToken token = await _azureServiceTokenProvider.GetTokenAsync(new TokenRequestContext(new string[] { AzureScope }, null), cancellationToken).ConfigureAwait(false);
        return token.Token;
    }
}
