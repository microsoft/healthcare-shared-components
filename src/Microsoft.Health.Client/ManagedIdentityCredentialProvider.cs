// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Health.Client
{
    public class ManagedIdentityCredentialProvider : CredentialProvider
    {
        private readonly ManagedIdentityCredentialConfiguration _managedIdentityCredentialConfiguration;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityCredentialProvider"/> class.
        /// This class is used to obtain a token for the configured resource via Managed Identity.
        /// </summary>
        /// <param name="managedIdentityCredentialConfiguration">The configuration of the token to obtain.</param>
        /// <param name="httpClientFactory">An optional <see cref="IHttpClientFactory"/> for use within the <see cref="AzureServiceTokenProvider"/>.</param>
        public ManagedIdentityCredentialProvider(ManagedIdentityCredentialConfiguration managedIdentityCredentialConfiguration, IHttpClientFactory httpClientFactory = null)
        {
            EnsureArg.IsNotNull(managedIdentityCredentialConfiguration);

            _managedIdentityCredentialConfiguration = managedIdentityCredentialConfiguration;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider(httpClientFactory: _httpClientFactory);
            return await azureServiceTokenProvider.GetAccessTokenAsync(_managedIdentityCredentialConfiguration.Resource, _managedIdentityCredentialConfiguration.TenantId, cancellationToken);
        }
    }
}
