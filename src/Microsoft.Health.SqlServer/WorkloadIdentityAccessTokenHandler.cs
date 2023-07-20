// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;
public class WorkloadIdentityAccessTokenHandler : IAccessTokenHandler
{
    private const string AzureResource = "https://database.windows.net/.default";

    public SqlServerAuthenticationType AuthenticationType => SqlServerAuthenticationType.WorkloadIdentity;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        WorkloadIdentityCredential credential = new WorkloadIdentityCredential();

        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { AzureResource }), CancellationToken.None).ConfigureAwait(false);

        return token.Token;
    }
}
