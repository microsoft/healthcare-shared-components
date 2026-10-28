// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core.ComponentModel.DataAnnotations;
using Microsoft.Health.SqlServer.Features.Client;

namespace Microsoft.Health.SqlServer.Configs;

/// <summary>
/// Represents the settings for connection to one or more databases as the provider for a data store.
/// </summary>
public class SqlServerDataStoreOptions
{
    /// <summary>
    /// The default section name used in configurations.
    /// </summary>
    public const string SectionName = "SqlServer";

    /// <summary>
    /// Gets or sets whether the schema initializer should attempt to create the database if not present.
    /// </summary>
    /// <value><c>true</c> if database creation is allowed; otherwise, <c>false</c>.</value>
    public bool AllowDatabaseCreation { get; set; }

    /// <summary>
    /// Gets or sets the timeout used for <see cref="SqlCommand.CommandTimeout"/>.
    /// </summary>
    /// <value>The command timeout. The default is 30 seconds.</value>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00", ConvertValueInInvariantCulture = true)]
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the connection string used to communicate with a database.
    /// </summary>
    /// <remarks>
    /// This property is only supported for single databases. Setting it replaces <see cref="Databases"/> with a single entry.
    /// </remarks>
    /// <value>
    /// The connection string of the single configured database, or <see langword="null"/> if zero or multiple databases are configured.
    /// </value>
    /// <exception cref="InvalidOperationException">The property is set when multiple databases have already been specified.</exception>
    public string ConnectionString
    {
        // The getter must not throw because options validation reads every public property
        get => Databases?.Count is 1 ? Databases.Values.Single().ConnectionString : null!;
        set
        {
            if (Databases?.Count > 1)
                throw new InvalidOperationException("Multiple connection strings have already been specified.");

            Databases = new Dictionary<string, SqlServerDatabaseOptions> { { "Default", new SqlServerDatabaseOptions { ConnectionString = value } } };
        }
    }

    /// <summary>
    /// Gets or sets the available databases keyed by a "friendly" name.
    /// </summary>
    /// <value>
    /// A dictionary of one or more databases.
    /// </value>
    [Required]
    [MinLength(1)]
    [ValidateDictionaryValues<string, SqlServerDatabaseOptions>]
    public IReadOnlyDictionary<string, SqlServerDatabaseOptions> Databases { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether the schema initializer should attempt to bring the schema to the minimum supported version.
    /// </summary>
    /// <value><c>true</c> if the schema should be initialized; otherwise, <c>false</c>.</value>
    public bool Initialize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of connections allowed in the pool when connecting to SQL.
    /// </summary>
    /// <value>
    /// The maximum pool size if specified; otherwise, <see langword="null"/> to use the default pool size.
    /// </value>
    [Range(1, SqlConstants.MaxPoolSizeLimit, ConvertValueInInvariantCulture = true)]
    public int? MaxPoolSize { get; set; }

    /// <summary>
    /// Gets or sets the Azure region the service is running in.
    /// </summary>
    /// <remarks>
    /// Used only as a dimension on emitted metrics (E.g. the schema-behind metric on read-only geo-replication secondaries).
    /// </remarks>
    /// <value>The name of the Azure region.</value>
    [Required(AllowEmptyStrings = false)]
    public string Region { get; set; } = default!;

    /// <summary>
    /// Gets or sets the configuration for the SQL client retry policy.
    /// </summary>
    /// <value>The SQL client retry options.</value>
    public SqlClientRetryOptions Retry { get; set; } = new SqlClientRetryOptions();

    /// <summary>
    /// Gets or sets the schema migration options.
    /// </summary>
    /// <value>The schema options.</value>
    [ValidateObjectMembers]
    public SqlServerSchemaOptions SchemaOptions { get; set; } = new SqlServerSchemaOptions();

    /// <summary>
    /// Gets or sets the statement timeout to set on the <c>ServerConnection</c>.
    /// </summary>
    /// <value>The statement timeout. The default is 4 hours.</value>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00", ConvertValueInInvariantCulture = true)]
    public TimeSpan StatementTimeout { get; set; } = TimeSpan.FromHours(4);
}
