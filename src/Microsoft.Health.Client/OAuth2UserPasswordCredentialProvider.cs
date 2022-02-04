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
    public class OAuth2UserPasswordCredentialProvider : CredentialProvider
    {
        private readonly IOptionsMonitor<OAuth2UserPasswordCredentialConfiguration> _oAuth2UserPasswordCredentialConfiguration;
        private readonly HttpClient _httpClient;
        private readonly string _optionsName;

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2UserPasswordCredentialProvider"/> class.
        /// This class is used to obtain a token for the configured resource via the OAuth2 token endpoint via user password.
        /// </summary>
        /// <param name="oAuth2UserCredentialConfigurationMonitor">The configuration to use when obtaining a token.</param>
        /// <param name="httpClient">The <see cref="HttpClient" /> to use when calling the token uri.</param>
        /// <param name="optionsName">Optional name to use when retrieving options from the IOptionsMonitor</param>
        public OAuth2UserPasswordCredentialProvider(IOptionsMonitor<OAuth2UserPasswordCredentialConfiguration> oAuth2UserCredentialConfigurationMonitor, HttpClient httpClient, string optionsName = null)
        {
            EnsureArg.IsNotNull(oAuth2UserCredentialConfigurationMonitor, nameof(oAuth2UserCredentialConfigurationMonitor));
            EnsureArg.IsNotNull(httpClient, nameof(httpClient));

            _httpClient = httpClient;
            _oAuth2UserPasswordCredentialConfiguration = oAuth2UserCredentialConfigurationMonitor;
            _optionsName = optionsName;
        }

        protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
        {
            OAuth2UserPasswordCredentialConfiguration oAuth2UserPasswordCredentialConfiguration = string.IsNullOrEmpty(_optionsName)
                ? _oAuth2UserPasswordCredentialConfiguration.CurrentValue
                : _oAuth2UserPasswordCredentialConfiguration.Get(_optionsName);
            var formData = new List<KeyValuePair<string, string>>
            {
                new (OpenIdConnectParameterNames.ClientId, oAuth2UserPasswordCredentialConfiguration.ClientId),
                new (OpenIdConnectParameterNames.ClientSecret, oAuth2UserPasswordCredentialConfiguration.ClientSecret),
                new (OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.Password),
                new (OpenIdConnectParameterNames.Scope, oAuth2UserPasswordCredentialConfiguration.Scope),
                new (OpenIdConnectParameterNames.Resource, oAuth2UserPasswordCredentialConfiguration.Resource),
                new (OpenIdConnectParameterNames.Username, oAuth2UserPasswordCredentialConfiguration.Username),
                new (OpenIdConnectParameterNames.Password, oAuth2UserPasswordCredentialConfiguration.Password),
            };

            using var formContent = new FormUrlEncodedContent(formData);
            using HttpResponseMessage tokenResponse = await _httpClient.PostAsync(oAuth2UserPasswordCredentialConfiguration.TokenUri, formContent, cancellationToken).ConfigureAwait(false);

            var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

            if (tokenResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new FailToRetrieveTokenException(openIdConnectMessage.ErrorDescription);
            }

            return openIdConnectMessage.AccessToken;
        }
    }
}
