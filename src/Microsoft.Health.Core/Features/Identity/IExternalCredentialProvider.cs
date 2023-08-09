// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Azure.Core;

namespace Microsoft.Health.Core.Features.Identity;

public interface IExternalCredentialProvider
{
    /// <summary>
    /// Retrieves the token credential used for external operations.
    /// </summary>
    /// <returns>
    /// The credential for the operation, if found; otherwise <see langword="null"/>.
    /// </returns>
    TokenCredential GetTokenCredential();
}
