// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// Shared logic for reporting the state of a read-only geo-replication secondary once
/// <see cref="ISchemaWriteGate"/> has denied a write. Used by both <see cref="SchemaInitializer"/>
/// and <see cref="Manager.SqlSchemaManager"/> so the two independent schema-write paths in this
/// package produce identical status determination, log messages, and metrics.
/// </summary>
internal static class SchemaWriteGateDiagnostics
{
    /// <summary>
    /// Compares the replicated schema version against the maximum version supported by the running
    /// instance to describe the state of a read-only geo-replication secondary.
    /// </summary>
    /// <param name="currentVersion">The schema version currently applied to the database, if known.</param>
    /// <param name="maximumSupportedVersion">The maximum schema version supported by the running instance, if known.</param>
    public static SecondarySchemaStatus GetSecondarySchemaStatus(int? currentVersion, int? maximumSupportedVersion)
    {
        if (!currentVersion.HasValue || !maximumSupportedVersion.HasValue)
        {
            return SecondarySchemaStatus.Unknown;
        }

        if (currentVersion < maximumSupportedVersion)
        {
            return SecondarySchemaStatus.Behind;
        }

        return currentVersion > maximumSupportedVersion ? SecondarySchemaStatus.Ahead : SecondarySchemaStatus.Current;
    }

    /// <summary>
    /// Logs the outcome of a write denied by <see cref="ISchemaWriteGate"/>, using the given
    /// <paramref name="status"/> to select the appropriate level and message.
    /// </summary>
    public static void LogReadOnlySecondaryStatus(ILogger logger, SecondarySchemaStatus status, int? currentVersion, int? maximumSupportedVersion)
    {
        switch (status)
        {
            case SecondarySchemaStatus.Unknown:
                logger.LogWarning("Schema write gate denied writes (read-only geo-replication secondary), but the current schema version could not be determined. Skipping schema upgrade.");
                break;
            case SecondarySchemaStatus.Behind:
                logger.LogInformation(
                    "Schema write gate denied writes (read-only geo-replication secondary). Schema is behind. Current version: {CurrentVersion}; latest supported version: {LatestVersion}. Skipping schema upgrade.",
                    currentVersion,
                    maximumSupportedVersion);
                break;
            case SecondarySchemaStatus.Ahead:
                logger.LogWarning(
                    "Schema write gate denied writes (read-only geo-replication secondary). The replicated schema (version {CurrentVersion}) is newer than the maximum version supported by this instance ({LatestVersion}); this instance may be running outdated code. Skipping schema upgrade.",
                    currentVersion,
                    maximumSupportedVersion);
                break;
            default:
                logger.LogInformation(
                    "Schema write gate denied writes (read-only geo-replication secondary). Schema is current at version {CurrentVersion}. Skipping schema upgrade.",
                    currentVersion);
                break;
        }
    }
}
