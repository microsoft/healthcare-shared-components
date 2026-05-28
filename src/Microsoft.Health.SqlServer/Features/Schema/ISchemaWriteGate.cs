// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// A gate that controls whether the schema job worker is permitted to write instance schema information.
/// </summary>
/// <remarks>
/// The default implementation returns <see langword="true"/>, which is appropriate for services that are
/// not configured for geo-replication. Services using geo-replication should provide an implementation
/// that returns <see langword="false"/> when the SQL database is a read-only secondary, so that the
/// schema worker skips writes entirely rather than attempting a write against a read-only replica.
/// </remarks>
public interface ISchemaWriteGate
{
    /// <summary>
    /// Determines whether the schema job worker is permitted to write instance schema information.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the write should proceed; <see langword="false"/> if the database
    /// is a read-only secondary and the write should be skipped.
    /// </returns>
    Task<bool> CanWriteAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
}
