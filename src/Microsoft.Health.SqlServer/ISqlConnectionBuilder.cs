// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

public interface ISqlConnectionBuilder
{
    /// <summary>
    /// Get unopened SqlConnection object.
    /// If initial catalog is not provided, it is determined from the connection string.
    /// </summary>
    /// <param name="initialCatalog">Initial catalog to connect to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>SqlConnection object.</returns>
    Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default);
}
