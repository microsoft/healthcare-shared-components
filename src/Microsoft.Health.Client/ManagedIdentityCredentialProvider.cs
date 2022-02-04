// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Health.Client
{
    public class ManagedIdentityCredentialProvider : CredentialProvider
    {
        private readonly IOptionsMonitor<ManagedIdentityCredentialConfiguration> _managedIdentityCredentialConfigurationMonitor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _optionsName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityCredentialProvider"/> class.
        /// This class is used to obtain a token for the configured resource via Managed Identity.
        /// </summary>
        /// <param name="managedIdentityCredentialConfigurationMonitorMonitor">The configuration of the token to obtain.</param>
        /// <param name="httpClientFactory">An optional <see cref="IHttpClientFactory"/> for use within the <see cref="AzureServiceTokenProvider"/>.</param>
        /// <param name="optionsName">Optional name to use when retrieving options from the IOptionsMonitor</param>
        public ManagedIdentityCredentialProvider(IOptionsMonitor<ManagedIdentityCredentialConfiguration> managedIdentityCredentialConfigurationMonitorMonitor, IHttpClientFactory httpClientFactory = null, string optionsName = null)
        {
            EnsureArg.IsNotNull(managedIdentityCredentialConfigurationMonitorMonitor, nameof(managedIdentityCredentialConfigurationMonitorMonitor));

            _managedIdentityCredentialConfigurationMonitor = managedIdentityCredentialConfigurationMonitorMonitor;
            _optionsName = optionsName;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            ManagedIdentityCredentialConfiguration managedIdentityCredentialConfiguration = string.IsNullOrEmpty(_optionsName)
                ? _managedIdentityCredentialConfigurationMonitor.CurrentValue
                : _managedIdentityCredentialConfigurationMonitor.Get(_optionsName);
            var azureServiceTokenProvider = new AzureServiceTokenProvider(httpClientFactory: _httpClientFactory);
            return await azureServiceTokenProvider.GetAccessTokenAsync(managedIdentityCredentialConfiguration.Resource, managedIdentityCredentialConfiguration.TenantId, cancellationToken);
        }
    }
}
