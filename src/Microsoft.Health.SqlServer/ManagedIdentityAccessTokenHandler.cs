// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

public class ManagedIdentityAccessTokenHandler : IAccessTokenHandler
{
    private readonly AzureServiceTokenProvider _azureServiceTokenProvider;

    public SqlServerAuthenticationType AuthenticationType => SqlServerAuthenticationType.ManagedIdentity;

    public string AzureScope => "https://database.windows.net/";

    public ManagedIdentityAccessTokenHandler(AzureServiceTokenProvider azureServiceTokenProvider)
    {
        EnsureArg.IsNotNull(azureServiceTokenProvider, nameof(azureServiceTokenProvider));

        _azureServiceTokenProvider = azureServiceTokenProvider;
    }

    /// <inheritdoc />
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        return _azureServiceTokenProvider.GetAccessTokenAsync(AzureScope, cancellationToken: cancellationToken);
    }
}
