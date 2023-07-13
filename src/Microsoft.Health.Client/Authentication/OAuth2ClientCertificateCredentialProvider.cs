// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Authentication.Exceptions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Health.Client.Authentication;

public class OAuth2ClientCertificateCredentialProvider : CredentialProvider
{
    private const string JwtAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
    private const string Rs256Algorithm = "RS256";

    private readonly IOptionsMonitor<OAuth2ClientCertificateCredentialOptions> _oAuth2ClientCertificateCredentialOptionsMonitor;
    private readonly HttpClient _httpClient;
    private readonly string _optionsName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2ClientCertificateCredentialProvider"/> class.
    /// This class is used to obtain a token for the configured resource via the OAuth2 token endpoint via client certificate credentials.
    /// </summary>
    /// <param name="oAuth2ClientCertificateCredentialOptionsMonitor">The configuration to use when obtaining a token.</param>
    /// <param name="httpClient">The <see cref="HttpClient" /> to use when calling the token uri.</param>
    /// <param name="optionsName">Optional name to use when retrieving options from the IOptionsMonitor</param>
    public OAuth2ClientCertificateCredentialProvider(IOptionsMonitor<OAuth2ClientCertificateCredentialOptions> oAuth2ClientCertificateCredentialOptionsMonitor, HttpClient httpClient, string optionsName = null)
    {
        EnsureArg.IsNotNull(oAuth2ClientCertificateCredentialOptionsMonitor, nameof(oAuth2ClientCertificateCredentialOptionsMonitor));
        EnsureArg.IsNotNull(httpClient, nameof(httpClient));

        _httpClient = httpClient;
        _oAuth2ClientCertificateCredentialOptionsMonitor = oAuth2ClientCertificateCredentialOptionsMonitor;
        _optionsName = optionsName;
    }

    protected override async Task<string> BearerTokenFunction(CancellationToken cancellationToken)
    {
        OAuth2ClientCertificateCredentialOptions oAuth2ClientCertificateCredentialOptions = _oAuth2ClientCertificateCredentialOptionsMonitor.Get(_optionsName);

        // Values specified for the assertion JWT specified here: https://docs.microsoft.com/azure/active-directory/develop/active-directory-certificate-credentials
        var additionalClaims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub,  oAuth2ClientCertificateCredentialOptions.ClientId),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var signingCredentials = new X509SigningCredentials(oAuth2ClientCertificateCredentialOptions.Certificate, Rs256Algorithm);
        var jwtSecurityToken = new JwtSecurityToken(
            issuer: oAuth2ClientCertificateCredentialOptions.ClientId,
            audience: oAuth2ClientCertificateCredentialOptions.TokenUri.AbsoluteUri,
            claims: additionalClaims,
            notBefore: DateTime.Now,
            expires: DateTime.Now.AddMinutes(10),
            signingCredentials: signingCredentials);

        string exportedCertificate = Convert.ToBase64String(oAuth2ClientCertificateCredentialOptions.Certificate.Export(X509ContentType.Cert));
        jwtSecurityToken.Header.Add(JwtHeaderParameterNames.X5c, exportedCertificate);

        var handler = new JwtSecurityTokenHandler();
        var encodedCert = handler.WriteToken(jwtSecurityToken);

        var formData = new List<KeyValuePair<string, string>>
        {
            new (OpenIdConnectParameterNames.ClientId, oAuth2ClientCertificateCredentialOptions.ClientId),
            new (OpenIdConnectParameterNames.ClientAssertionType, JwtAssertionType),
            new (OpenIdConnectParameterNames.GrantType, OpenIdConnectGrantTypes.ClientCredentials),
            new (OpenIdConnectParameterNames.Scope, oAuth2ClientCertificateCredentialOptions.Scope),
            new (OpenIdConnectParameterNames.Resource, oAuth2ClientCertificateCredentialOptions.Resource),
            new (OpenIdConnectParameterNames.ClientAssertion, encodedCert),
        };

        using var formContent = new FormUrlEncodedContent(formData);
        using HttpResponseMessage tokenResponse = await _httpClient.PostAsync(oAuth2ClientCertificateCredentialOptions.TokenUri, formContent, cancellationToken).ConfigureAwait(false);

        var openIdConnectMessage = new OpenIdConnectMessage(await tokenResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));

        if (tokenResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            throw new FailToRetrieveTokenException(openIdConnectMessage.ErrorDescription);
        }

        return openIdConnectMessage.AccessToken;
    }
}
