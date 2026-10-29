// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents an <see cref="ISqlConnectionBuilder"/> that creates connections for the database configured in the
/// <see cref="SqlServerDataStoreOptions"/> whose key matches the current <see cref="ISqlServerDatabaseContext"/>.
/// </summary>
public class ScopedSqlConnectionBuilder : SqlConnectionBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedSqlConnectionBuilder"/> class.
    /// </summary>
    /// <param name="options">The data store options containing one or more database configurations.</param>
    /// <param name="context">The context whose <see cref="ISqlServerDatabaseContext.Key"/> identifies the database to use.</param>
    /// <param name="retryProvider">The retry logic provider assigned to each created connection.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/>, its databases, <paramref name="context"/>, or <paramref name="retryProvider"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="options"/> does not contain a database configuration for the key specified by <paramref name="context"/>.
    /// </exception>
    public ScopedSqlConnectionBuilder(IOptionsSnapshot<SqlServerDataStoreOptions> options, ISqlServerDatabaseContext context, SqlRetryLogicBaseProvider retryProvider)
        : base(FindDatabaseOptions(EnsureArg.IsNotNull(options?.Value?.Databases, nameof(options)), EnsureArg.IsNotNull(context, nameof(context))), retryProvider)
    { }

    private static SqlServerDatabaseOptions FindDatabaseOptions(IReadOnlyDictionary<string, SqlServerDatabaseOptions> databases, ISqlServerDatabaseContext context)
    {
        if (!databases.TryGetValue(context.Key, out SqlServerDatabaseOptions? databaseOptions))
            throw new ArgumentException($"No database configuration found for key '{context.Key}'.", nameof(context));

        return databaseOptions;
    }
}
