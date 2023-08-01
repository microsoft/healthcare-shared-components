// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Configs;

public class SqlServerDataStoreConfiguration
{
    /// <summary>
    /// The default section name used in configurations.
    /// </summary>
    public const string SectionName = "SqlServer";

    /// <summary>
    /// The SQL Server connection string.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Allows the schema initializer to attempt to bring the schema to the minimum supported version.
    /// </summary>
    public bool Initialize { get; set; }

    /// <summary>
    /// Allows the schema initializer to attempt to create the database if not present.
    /// </summary>
    public bool AllowDatabaseCreation { get; set; }

    /// <summary>
    /// Authentication type.
    /// </summary>
    public SqlServerAuthenticationType AuthenticationType { get; set; } = SqlServerAuthenticationType.ConnectionString;

    /// <summary>
    /// Configuration for Sql Client retry policy.
    /// </summary>
    public SqlClientRetryOptions Retry { get; set; } = new SqlClientRetryOptions();

    /// <summary>
    /// Updates the schema migration options
    /// </summary>
    public SqlServerSchemaOptions SchemaOptions { get; set; } = new SqlServerSchemaOptions();

    /// <summary>
    /// If set, Instructs the service to terminate when its schema reaches the specified version.
    /// </summary>
    public int? TerminateWhenSchemaVersionUpdatedTo { get; set; }

    /// <summary>
    /// If set, the client id of the managed identity to use when connecting to SQL, if AuthenticationType == ManagedIdentity.
    /// </summary>
    public string ManagedIdentityClientId { get; set; }

    /// <summary>
    /// If set, represents the tenantId.
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// Specifies the statement timeout to set on ServerConnection.
    /// </summary>
    public TimeSpan StatementTimeout { get; set; } = TimeSpan.FromSeconds(14400);

    /// <summary>
    /// Specifies the SqlCommand.CommandTimeout
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// If set, the maximum number of connections allowed in the pool to use when connecting to SQL.
    /// </summary>
    public int? MaxPoolSize { get; set; }
}
