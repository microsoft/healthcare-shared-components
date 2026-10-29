// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents the context for the current database connection, including the key used to identify the database in the configuration.
/// </summary>
public interface ISqlServerDatabaseContext
{
    /// <summary>
    /// Gets the database key used to identify the database in the configuration.
    /// </summary>
    /// <value>A friendly name for the database. It may not be the actual database name.</value>
    string Key { get; }
}
