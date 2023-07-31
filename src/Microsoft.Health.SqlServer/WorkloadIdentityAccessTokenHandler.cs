// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

public class WorkloadIdentityAccessTokenHandler : IAccessTokenHandler
{
    public SqlServerAuthenticationType AuthenticationType => SqlServerAuthenticationType.WorkloadIdentity;

    public string AzureScope => "https://database.windows.net/.default";

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Creates a new instance of the WorkloadIdentityCredential with the default options.
        // When no options are specified AZURE_TENANT_ID, AZURE_CLIENT_ID and AZURE_FEDERATED_TOKEN_FILE must be specified in the environment.
        WorkloadIdentityCredential credential = new WorkloadIdentityCredential();

        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { AzureScope }), cancellationToken).ConfigureAwait(false);

        return token.Token;
    }
}
