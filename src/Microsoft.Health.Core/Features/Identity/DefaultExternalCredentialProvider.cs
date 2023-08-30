// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;

namespace Microsoft.Health.Core.Features.Identity;

internal sealed class DefaultExternalCredentialProvider : IExternalCredentialProvider
{
    public TokenCredential GetTokenCredential()
        => new DefaultAzureCredential(includeInteractiveCredentials: false);
}
