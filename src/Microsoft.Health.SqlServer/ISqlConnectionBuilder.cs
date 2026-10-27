// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents a builder of SQL connections.
/// </summary>
public interface ISqlConnectionBuilder
{
    /// <summary>
    /// Gets a SQL connection as additionally configured by the caller.
    /// </summary>
    /// <param name="configure">An optional delegate for configuring the connection.</param>
    /// <returns>The SQL connection.</returns>
    SqlConnection CreateConnection(Action<SqlConnectionStringBuilder> configure = null);
}
