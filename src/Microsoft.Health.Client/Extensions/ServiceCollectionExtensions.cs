// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Net.Http;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Health.Client.Authentication;
using Microsoft.Health.Extensions.DependencyInjection;

namespace Microsoft.Health.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddNamedManagedIdentityCredentialProvider(this IServiceCollection serviceCollection, IConfiguration managedIdentityCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(managedIdentityCredentialConfiguration, nameof(managedIdentityCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<ManagedIdentityCredentialOptions>(name, managedIdentityCredentialConfiguration);

        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<ManagedIdentityCredentialOptions> options = provider.GetService<IOptionsMonitor<ManagedIdentityCredentialOptions>>();
                var credentialProvider = new ManagedIdentityCredentialProvider(options, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }

    public static void AddNamedOAuth2ClientCertificateCredentialProvider(this IServiceCollection serviceCollection, IConfiguration oAuth2ClientCertificateCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(oAuth2ClientCertificateCredentialConfiguration, nameof(oAuth2ClientCertificateCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<OAuth2ClientCertificateCredentialOptions>(name, oAuth2ClientCertificateCredentialConfiguration);

        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<OAuth2ClientCertificateCredentialOptions> options = provider.GetService<IOptionsMonitor<OAuth2ClientCertificateCredentialOptions>>();
                var httpClient = new HttpClient();
                var credentialProvider = new OAuth2ClientCertificateCredentialProvider(options, httpClient, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }

    public static void AddNamedOAuth2ClientCredentialProvider(this IServiceCollection serviceCollection, IConfiguration oAuth2ClientCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(oAuth2ClientCredentialConfiguration, nameof(oAuth2ClientCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<OAuth2ClientCredentialOptions>(name, oAuth2ClientCredentialConfiguration);
        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<OAuth2ClientCredentialOptions> options = provider.GetService<IOptionsMonitor<OAuth2ClientCredentialOptions>>();
                var httpClient = new HttpClient();
                var credentialProvider = new OAuth2ClientCredentialProvider(options, httpClient, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }

    public static void AddNamedOAuth2UserPasswordCredentialProvider(this IServiceCollection serviceCollection, IConfiguration oAuth2UserPasswordCredentialConfiguration, string name)
    {
        EnsureArg.IsNotNull(serviceCollection, nameof(serviceCollection));
        EnsureArg.IsNotNull(oAuth2UserPasswordCredentialConfiguration, nameof(oAuth2UserPasswordCredentialConfiguration));
        EnsureArg.IsNotNullOrWhiteSpace(name, nameof(name));

        serviceCollection.Configure<OAuth2UserPasswordCredentialOptions>(name, oAuth2UserPasswordCredentialConfiguration);
        serviceCollection.Add(provider =>
            {
                IOptionsMonitor<OAuth2UserPasswordCredentialOptions> options = provider.GetService<IOptionsMonitor<OAuth2UserPasswordCredentialOptions>>();
                var httpClient = new HttpClient();
                var credentialProvider = new OAuth2UserPasswordCredentialProvider(options, httpClient, name);
                return new NamedCredentialProvider(name, credentialProvider);
            })
            .Singleton()
            .AsSelf();
    }
}
