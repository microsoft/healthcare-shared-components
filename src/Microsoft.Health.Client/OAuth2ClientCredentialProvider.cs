// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Exceptions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Health.Client
{
    public class OAuth2ClientCredentialProvider : CredentialProvider
    {
        private readonly IOptionsMonitor<OAuth2ClientCredentialConfiguration> _oAuth2ClientCredentialConfigurationMonitor;
        private readonly HttpClient _httpClient;
        private readonly string _optionsName;

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2ClientCredentialProvider"/> class.
        /// This class is used to obtain a token for the configured resource via the OAuth2 token endpoint via client credentials.
        /// </summary>
        /// <param name="oAuth2ClientCredentialConfigurationMonitor">The configuration to use when obtaining a token.</param>
        /// <param name="httpClient">The <see cref="HttpClient" /> to use when calling the token uri.</param>
        /// <param name="optionsName">Optional name to use when retrieving options from the IOptionsMonitor</param>
        public OAuth2ClientCredentialProvider(IOptionsMonitor<OAuth2ClientCredentialConfiguration> oAuth2ClientCredentialConfigurationMonitor, HttpClient httpClient, string optionsName = null)
        {
            EnsureArg.IsNotNull(oAuth2ClientCredentialConfigurationMonitor, nameof(oAuth2ClientCredentialConfigurationMonitor));
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));

            _httpClient = httpClient;
            _oAuth2ClientCredentialConfigurationMonitor = oAuth2ClientCredentialConfigurationMonitor;
            _optionsName = optionsName;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            OAuth2ClientCredentialConfiguration oAuth2ClientCredentialConfiguration = string.IsNullOrEmpty(_optionsName)
                ? _oAuth2ClientCredentialConfigurationMonitor.CurrentValue
                : _oAuth2ClientCredentialConfigurationMonitor.Get(_optionsName);

            var formData = new List<KeyValuePair<string, string>>
            {
                new (OpenIdConnectParameterNames.ClientId, oAuth2ClientCredentialConfiguration.ClientId),
                new (OpenIdConnectParameterNames.ClientSecret, oAuth2ClientCredentialConfiguration.ClientSecret),
                new (OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials),
                new (OpenIdConnectParameterNames.Scope, oAuth2ClientCredentialConfiguration.Scope),
                new (OpenIdConnectParameterNames.Resource, oAuth2ClientCredentialConfiguration.Resource),
            };

            using var formContent = new FormUrlEncodedContent(formData);
            using HttpResponseMessage tokenResponse = await _httpClient.PostAsync(oAuth2ClientCredentialConfiguration.TokenUri, formContent, cancellationToken).ConfigureAwait(false);

            var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            if (tokenResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new FailToRetrieveTokenException(openIdConnectMessage.ErrorDescription);
            }

            return openIdConnectMessage.AccessToken;
        }
    }
}
