// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.Client.Authentication;

public interface ICredentialProvider
{
    Task<string> GetBearerTokenAsync(CancellationToken cancellationToken = default);
}
