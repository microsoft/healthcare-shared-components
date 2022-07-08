// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Client.Authentication;

public class ManagedIdentityCredentialOptions
{
    public ManagedIdentityCredentialOptions()
    {
    }

    public ManagedIdentityCredentialOptions(string resource, string tenantId)
    {
        EnsureArg.IsNotNullOrWhiteSpace(resource, nameof(resource));
        EnsureArg.IsNotNullOrWhiteSpace(tenantId, nameof(tenantId));

        Resource = resource;
        TenantId = tenantId;
    }

    public ManagedIdentityCredentialOptions(string resource, string tenantId, string clientId)
    {
        EnsureArg.IsNotNullOrWhiteSpace(resource, nameof(resource));
        EnsureArg.IsNotNullOrWhiteSpace(tenantId, nameof(tenantId));
        EnsureArg.IsNotNullOrWhiteSpace(clientId, nameof(clientId));

        Resource = resource;
        TenantId = tenantId;
        ClientId = clientId;
    }

    public string Resource { get; set; }

    public string TenantId { get; set; }

    public string ClientId { get; set; }
}
