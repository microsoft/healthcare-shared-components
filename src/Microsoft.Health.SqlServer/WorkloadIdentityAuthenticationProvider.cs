// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

internal sealed class WorkloadIdentityAuthenticationProvider : SqlAuthenticationProvider
{
    private const string DefaultScopeSuffix = "/.default";

    // SQL Client doesn't integrate with DI today, so we'll use this factory for testing
    private readonly Func<WorkloadIdentityCredentialOptions, WorkloadIdentityCredential> _createCredential;

    public WorkloadIdentityAuthenticationProvider()
        : this(o => new WorkloadIdentityCredential(o))
    { }

    internal WorkloadIdentityAuthenticationProvider(Func<WorkloadIdentityCredentialOptions, WorkloadIdentityCredential> createCredential)
        => _createCredential = EnsureArg.IsNotNull(createCredential, nameof(createCredential));

    public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
    {
        // Logic is based on the existing ActiveDirectoryAuthenticationProvider
        // https://github.com/dotnet/SqlClient/blob/111033e65625c435295cea500508a13e00a3d764/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/ActiveDirectoryAuthenticationProvider.cs
        EnsureArg.IsNotNull(parameters, nameof(parameters));

        using CancellationTokenSource cts = new();
        if (parameters.ConnectionTimeout > 0)
            cts.CancelAfter(parameters.ConnectionTimeout * 1000); // Convert to milliseconds

        string scope = parameters.Resource.EndsWith(DefaultScopeSuffix, StringComparison.Ordinal) ? parameters.Resource : parameters.Resource + DefaultScopeSuffix;
        string[] scopes = new string[] { scope };
        TokenRequestContext tokenRequestContext = new(scopes);

        int seperatorIndex = parameters.Authority.LastIndexOf('/');
        string authority = parameters.Authority.Remove(seperatorIndex + 1);
        string clientId = string.IsNullOrWhiteSpace(parameters.UserId) ? null : parameters.UserId;

        WorkloadIdentityCredentialOptions options = new()
        {
            AuthorityHost = new Uri(authority),
            ClientId = clientId,
        };

        AccessToken accessToken = await _createCredential(options).GetTokenAsync(tokenRequestContext, cts.Token).ConfigureAwait(false);
        return new SqlAuthenticationToken(accessToken.Token, accessToken.ExpiresOn);
    }

    public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
        => authenticationMethod is SqlAuthenticationMethod.ActiveDirectoryManagedIdentity or SqlAuthenticationMethod.ActiveDirectoryMSI;
}
