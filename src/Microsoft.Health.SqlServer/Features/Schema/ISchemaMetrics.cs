// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// Emits metrics related to the state of the database schema.
/// </summary>
public interface ISchemaMetrics
{
    /// <summary>
    /// Records that a database was found running a schema version behind the latest supported version
    /// (for example, a read-only geo-replication secondary that cannot be upgraded in-service).
    /// </summary>
    /// <param name="databaseName">The name of the database that is behind.</param>
    /// <param name="schemaVersion">The schema version the database is currently on.</param>
    /// <param name="region">The Azure region the service is running in.</param>
    void SchemaBehind(string databaseName, int schemaVersion, string region);
}
