// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Represents an <see cref="ISqlConnectionBuilder"/> that creates connections for the single database
/// configured in the <see cref="SqlServerDataStoreOptions"/>.
/// </summary>
public class DefaultSqlConnectionBuilder : SqlConnectionBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSqlConnectionBuilder"/> class.
    /// </summary>
    /// <param name="options">The data store options containing exactly one database configuration.</param>
    /// <param name="retryProvider">The retry logic provider assigned to each created connection.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/>, its value, or <paramref name="retryProvider"/> is <see langword="null"/>,
    /// or <paramref name="options"/> does not contain any database configuration.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> contains more than one database configuration.</exception>
    public DefaultSqlConnectionBuilder(IOptions<SqlServerDataStoreOptions> options, SqlRetryLogicBaseProvider retryProvider)
        : base(EnsureArg.IsNotNull(options?.Value, nameof(options)).Databases?.Values.FirstOrDefault()!, retryProvider)
    {
        if (options.Value.Databases.Count > 1)
            throw new ArgumentException("Multiple databases may not be specified.", nameof(options));
    }
}
