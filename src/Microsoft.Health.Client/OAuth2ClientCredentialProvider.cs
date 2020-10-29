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
        private readonly OAuth2ClientCredentialConfiguration _oAuth2ClientCredentialConfiguration;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2ClientCredentialProvider"/> class.
        /// This class is used to obtain a token for the configured resource via the OAuth2 token endpoint via client credentials.
        /// </summary>
        /// <param name="oAuth2ClientCredentialConfiguration">The configuration to use when obtaining a token.</param>
        /// <param name="httpClient">The <see cref="HttpClient" /> to use when calling the token uri.</param>
        public OAuth2ClientCredentialProvider(IOptions<OAuth2ClientCredentialConfiguration> oAuth2ClientCredentialConfiguration, HttpClient httpClient)
        {
            EnsureArg.IsNotNull(oAuth2ClientCredentialConfiguration?.Value, nameof(oAuth2ClientCredentialConfiguration));
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));

            _httpClient = httpClient;
            _oAuth2ClientCredentialConfiguration = oAuth2ClientCredentialConfiguration.Value;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientId, _oAuth2ClientCredentialConfiguration.ClientId),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.ClientSecret, _oAuth2ClientCredentialConfiguration.ClientSecret),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Scope, _oAuth2ClientCredentialConfiguration.Scope),
                new KeyValuePair<string, string>(OpenIdConnectParameterNames.Resource, _oAuth2ClientCredentialConfiguration.Resource),
            };

            using var formContent = new FormUrlEncodedContent(formData);
            using HttpResponseMessage tokenResponse = await _httpClient.PostAsync(_oAuth2ClientCredentialConfiguration.TokenUri, formContent, cancellationToken);

            var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync());
            if (openIdConnectMessage.AccessToken == null)
            {
                throw new FailToRetrieveTokenException(openIdConnectMessage.Error);
            }

            return openIdConnectMessage.AccessToken;
        }
    }
}
