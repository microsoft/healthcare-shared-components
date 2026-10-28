// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents a builder of SQL connections.
/// </summary>
public interface ISqlConnectionBuilder
{
    /// <summary>
    /// Gets the name of the default database for connections created by this builder.
    /// </summary>
    /// <value>
    /// The initial catalog specified by the underlying connection string, or an empty string if none was specified.
    /// </value>
    string DefaultDatabase { get; }

    /// <summary>
    /// Creates a new SQL connection, optionally customizing its connection string.
    /// </summary>
    /// <remarks>
    /// The returned connection has not been opened, and the caller is responsible for disposing it.
    /// </remarks>
    /// <param name="configure">
    /// An optional delegate for modifying the <see cref="SqlConnectionStringBuilder"/> before the connection is created.
    /// For example, it may be used to change the <see cref="SqlConnectionStringBuilder.InitialCatalog"/>.
    /// </param>
    /// <returns>A new, unopened <see cref="SqlConnection"/>.</returns>
    SqlConnection CreateConnection(Action<SqlConnectionStringBuilder>? configure = null);
}
