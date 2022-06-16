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
using Microsoft.Health.Client.Authentication.Exceptions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.Health.Client.Authentication;

public class OAuth2ClientCredentialProvider : CredentialProvider
{
    private readonly IOptionsMonitor<OAuth2ClientCredentialOptions> _oAuth2ClientCredentialOptionsMonitor;
    private readonly HttpClient _httpClient;
    private readonly string _optionsName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2ClientCredentialProvider"/> class.
    /// This class is used to obtain a token for the configured resource via the OAuth2 token endpoint via client credentials.
    /// </summary>
    /// <param name="oAuth2ClientCredentialOptionsMonitor">The configuration to use when obtaining a token.</param>
    /// <param name="httpClient">The <see cref="HttpClient" /> to use when calling the token uri.</param>
    /// <param name="optionsName">Optional name to use when retrieving options from the IOptionsMonitor</param>
    public OAuth2ClientCredentialProvider(IOptionsMonitor<OAuth2ClientCredentialOptions> oAuth2ClientCredentialOptionsMonitor, HttpClient httpClient, string optionsName = null)
    {
        EnsureArg.IsNotNull(oAuth2ClientCredentialOptionsMonitor, nameof(oAuth2ClientCredentialOptionsMonitor));
        EnsureArg.IsNotNull(httpClient, nameof(httpClient));

        _httpClient = httpClient;
        _oAuth2ClientCredentialOptionsMonitor = oAuth2ClientCredentialOptionsMonitor;
        _optionsName = optionsName;
    }

    protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
    {
        OAuth2ClientCredentialOptions oAuth2ClientCredentialOptions = _oAuth2ClientCredentialOptionsMonitor.Get(_optionsName);

        var formData = new List<KeyValuePair<string, string>>
        {
            new (OpenIdConnectParameterNames.ClientId, oAuth2ClientCredentialOptions.ClientId),
            new (OpenIdConnectParameterNames.ClientSecret, oAuth2ClientCredentialOptions.ClientSecret),
            new (OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials),
            new (OpenIdConnectParameterNames.Scope, oAuth2ClientCredentialOptions.Scope),
            new (OpenIdConnectParameterNames.Resource, oAuth2ClientCredentialOptions.Resource),
        };

        using var formContent = new FormUrlEncodedContent(formData);
        using HttpResponseMessage tokenResponse = await _httpClient.PostAsync(oAuth2ClientCredentialOptions.TokenUri, formContent, cancellationToken).ConfigureAwait(false);

        var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        if (tokenResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            throw new FailToRetrieveTokenException(openIdConnectMessage.ErrorDescription);
        }

        return openIdConnectMessage.AccessToken;
    }
}
