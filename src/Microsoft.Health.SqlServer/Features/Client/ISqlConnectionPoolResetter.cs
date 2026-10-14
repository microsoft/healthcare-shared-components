// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Client;

/// <summary>
/// Abstraction over SQL connection pool clearing to enable unit testing.
/// </summary>
public interface ISqlConnectionPoolResetter
{
    /// <summary>
    /// Clears all SQL connection pools in the current process.
    /// </summary>
    void ClearAllPools();
}
