// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Client.Authentication;

public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; set; }

    public AuthenticationType? AuthenticationType { get; set; }

    public OAuth2ClientCertificateCredentialOptions OAuth2ClientCertificateCredential { get; set; }

    public OAuth2ClientCredentialOptions OAuth2ClientCredential { get; set; }

    public OAuth2UserPasswordCredentialOptions OAuth2UserPasswordCredential { get; set; }

    public ManagedIdentityCredentialOptions ManagedIdentityCredential { get; set; }
}
