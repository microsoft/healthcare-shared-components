// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Health.Client.Configuration;

namespace Microsoft.Health.Client.Extensions;

public static class IHttpClientBuilderRegistrationExtensions
{
    public static void AddAuthenticationHandler(this IHttpClientBuilder httpClientBuilder, IConfigurationSection authenticationConfigurationSection, string credentialProviderName = null)
    {
        EnsureArg.IsNotNull(httpClientBuilder, nameof(httpClientBuilder));
        EnsureArg.IsNotNull(authenticationConfigurationSection, nameof(authenticationConfigurationSection));

        if (string.IsNullOrWhiteSpace(credentialProviderName))
        {
            credentialProviderName = Guid.NewGuid().ToString();
        }

        var auth = new AuthenticationConfiguration();
        authenticationConfigurationSection.Bind(auth);
        if (!auth.Enabled)
        {
            return;
        }

        switch (auth.AuthenticationType)
        {
            case AuthenticationType.ManagedIdentity:
                httpClientBuilder.Services.AddNamedManagedIdentityCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.ManagedIdentityCredential)), credentialProviderName);
                break;
            case AuthenticationType.OAuth2ClientCertificateCredential:
                httpClientBuilder.Services.AddNamedOAuth2ClientCertificateCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.OAuth2ClientCertificateCredential)), credentialProviderName);
                break;
            case AuthenticationType.OAuth2ClientCredential:
                httpClientBuilder.Services.AddNamedOAuth2ClientCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.OAuth2ClientCredential)), credentialProviderName);
                break;
            case AuthenticationType.OAuth2UserPasswordCredential:
                httpClientBuilder.Services.AddNamedOAuth2UserPasswordCredentialProvider(authenticationConfigurationSection.GetSection(nameof(auth.OAuth2UserPasswordCredential)), credentialProviderName);
                break;
        }

        httpClientBuilder
            .AddHttpMessageHandler(x => new AuthenticationHttpMessageHandler(x.ResolveNamedCredentialProvider(credentialProviderName)));
    }
}
