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
    /// <summary>
    /// Determines the type of SqlServiceAuthenticationType. This correlates with the type of token credential that will be used by the implementation.
    /// </summary>
    SqlServerAuthenticationType AuthenticationType { get; }

    /// <summary>
    /// Determines the scope that is needed by the type of tokencredential used in the given implementation
    /// </summary>
    string AzureScope { get; }

    /// <summary>
    /// Get access token for the resource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task returning token.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
