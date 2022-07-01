// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;

namespace Microsoft.Health.Client.Authentication;

public class OAuth2UserPasswordCredentialOptions : OAuth2ClientCredentialOptions
{
    public OAuth2UserPasswordCredentialOptions()
    {
    }

    public OAuth2UserPasswordCredentialOptions(Uri tokenUri, string resource, string scope, string clientId, string clientSecret, string username, string password)
        : base(tokenUri, resource, scope, clientId, clientSecret)
    {
        EnsureArg.IsNotNullOrWhiteSpace(username, nameof(username));
        EnsureArg.IsNotNullOrWhiteSpace(password, nameof(password));

        Username = username;
        Password = password;
    }

    public string Username { get; set; }

    public string Password { get; set; }
}
