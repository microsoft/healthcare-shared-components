// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;

namespace Microsoft.Health.Client.Configuration;

public class ManagedIdentityCredentialConfiguration
{
    public ManagedIdentityCredentialConfiguration()
    {
    }

    public ManagedIdentityCredentialConfiguration(string resource, string tenantId)
    {
        EnsureArg.IsNotNullOrWhiteSpace(resource, nameof(resource));
        EnsureArg.IsNotNullOrWhiteSpace(tenantId, nameof(tenantId));

        Resource = resource;
        TenantId = tenantId;
    }

    public string Resource { get; set; }

    public string TenantId { get; set; }
}
