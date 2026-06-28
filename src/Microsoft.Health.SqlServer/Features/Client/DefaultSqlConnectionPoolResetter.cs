// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Features.Client;

/// <summary>
/// Default implementation that delegates to <see cref="SqlConnection.ClearAllPools"/>.
/// </summary>
internal sealed class DefaultSqlConnectionPoolResetter : ISqlConnectionPoolResetter
{
    public void ClearAllPools()
    {
        SqlConnection.ClearAllPools();
    }
}
