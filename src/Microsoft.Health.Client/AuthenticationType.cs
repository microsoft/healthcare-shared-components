// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Client
{
    public enum AuthenticationType
    {
        None = 0,
        ManagedIdentity = 1,
        OAuth2ClientCredential = 2,
        OAuth2UserPasswordCredential = 3,
        OAuth2ClientCertificateCredential = 4,
    }
}
