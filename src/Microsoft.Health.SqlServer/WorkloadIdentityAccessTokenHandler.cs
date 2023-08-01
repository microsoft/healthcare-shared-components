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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

public class WorkloadIdentityAccessTokenHandler : IAccessTokenHandler
{
    public SqlServerAuthenticationType AuthenticationType => SqlServerAuthenticationType.WorkloadIdentity;

    public string AzureScope => "https://database.windows.net/.default";

    private const string TokenFilePath = "/var/run/secrets/azure/tokens/azure-identity-token";
    private readonly WorkloadIdentityCredential _credential;
    private readonly ILogger<WorkloadIdentityAccessTokenHandler> _logger;

    public WorkloadIdentityAccessTokenHandler(IOptions<SqlServerDataStoreConfiguration> options, ILogger<WorkloadIdentityAccessTokenHandler> logger)
    {
        EnsureArg.IsNotNull(options, nameof(options));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));

        string managedIdentityClientId = options.Value.ManagedIdentityClientId;
        string tenantId = options?.Value.TenantId;

        _credential = new WorkloadIdentityCredential();
        _logger.LogInformation("The provided managedidentity is {ManagedIdentityClientId}", managedIdentityClientId);
        _logger.LogInformation("The provided tenantId is {TenantId}", tenantId);

        WorkloadIdentityCredentialOptions workloadIdentityCredentialOptions = new WorkloadIdentityCredentialOptions
        {
            TenantId = options?.Value.TenantId,
            ClientId = options?.Value.ManagedIdentityClientId,
            TokenFilePath = TokenFilePath,
        };

        _credential = new WorkloadIdentityCredential(workloadIdentityCredentialOptions);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _credential.GetTokenAsync(new TokenRequestContext(new[] { AzureScope }), cancellationToken).ConfigureAwait(false);
            return token.Token;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Encountered sql exception while fetching token to connect to sql database");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Encountered taskcancelled exception while fetching token to connect to sql database.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excountered an unknown exception while fetching token to connect to sql database");
            throw;
        }
    }
}
