// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.SqlServer.Configs;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer;

public interface IAccessTokenHandler
{
    SqlServerAuthenticationType AuthenticationType { get; }

    /// <summary>
    /// Get access token for the resource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task returning token.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
