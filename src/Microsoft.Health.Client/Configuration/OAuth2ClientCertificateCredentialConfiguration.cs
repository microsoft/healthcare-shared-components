// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography.X509Certificates;
using EnsureThat;

namespace Microsoft.Health.Client.Configuration;

public class OAuth2ClientCertificateCredentialConfiguration
{
    public OAuth2ClientCertificateCredentialConfiguration()
    {
    }

    public OAuth2ClientCertificateCredentialConfiguration(Uri tokenUri, string resource, string scope, string clientId, X509Certificate2 certificate)
    {
        EnsureArg.IsNotNull(tokenUri, nameof(tokenUri));
        EnsureArg.IsNotNullOrWhiteSpace(resource, nameof(resource));
        EnsureArg.IsNotNullOrWhiteSpace(scope, nameof(scope));
        EnsureArg.IsNotNullOrWhiteSpace(clientId, nameof(clientId));
        EnsureArg.IsNotNull(certificate, nameof(certificate));

        TokenUri = tokenUri;
        Resource = resource;
        Scope = scope;
        ClientId = clientId;
        Certificate = certificate;
    }

    public Uri TokenUri { get; set; }

    public string Resource { get; set; }

    public string Scope { get; set; }

    public string ClientId { get; set; }

    public X509Certificate2 Certificate { get; set; }
}
