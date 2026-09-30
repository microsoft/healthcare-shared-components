// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// Evaluates whether a schema upgrade may run and reports the state of a read-only
/// geo-replication secondary for both schema-write paths.
/// </summary>
public sealed class SchemaWriteGateEvaluator
{
    private readonly ISchemaWriteGate _writeGate;
    private readonly ISchemaMetrics _schemaMetrics;
    private readonly ISqlConnectionBuilder _sqlConnectionBuilder;
    private readonly IOptions<SqlServerDataStoreConfiguration> _options;
    private readonly ILogger<SchemaWriteGateEvaluator> _logger;

    public SchemaWriteGateEvaluator(
        ISchemaWriteGate writeGate,
        ISchemaMetrics schemaMetrics,
        ISqlConnectionBuilder sqlConnectionBuilder,
        IOptions<SqlServerDataStoreConfiguration> options,
        ILogger<SchemaWriteGateEvaluator> logger)
    {
        _writeGate = EnsureArg.IsNotNull(writeGate, nameof(writeGate));
        _schemaMetrics = EnsureArg.IsNotNull(schemaMetrics, nameof(schemaMetrics));
        _sqlConnectionBuilder = EnsureArg.IsNotNull(sqlConnectionBuilder, nameof(sqlConnectionBuilder));
        EnsureArg.IsNotNull(options?.Value, nameof(options));
        _options = options;
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    public async Task<bool> CanApplySchemaUpdatesAsync(
        Func<CancellationToken, Task<(int? CurrentVersion, int? MaximumSupportedVersion)>> getVersionsAsync,
        CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(getVersionsAsync, nameof(getVersionsAsync));
        if (await _writeGate.CanWriteAsync(cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        (int? currentVersion, int? maximumSupportedVersion) = await getVersionsAsync(cancellationToken).ConfigureAwait(false);
        SecondarySchemaStatus status = GetSecondarySchemaStatus(currentVersion, maximumSupportedVersion);
        LogReadOnlySecondaryStatus(status, currentVersion, maximumSupportedVersion);

        if (status == SecondarySchemaStatus.Behind)
        {
            _schemaMetrics.SchemaBehind(_sqlConnectionBuilder.DefaultDatabase, currentVersion.Value, _options.Value.Region);
        }

        return false;
    }

    /// <summary>
    /// Compares the replicated schema version against the maximum version supported by the running
    /// instance to describe the state of a read-only geo-replication secondary.
    /// </summary>
    /// <param name="currentVersion">The schema version currently applied to the database, if known.</param>
    /// <param name="maximumSupportedVersion">The maximum schema version supported by the running instance, if known.</param>
    internal static SecondarySchemaStatus GetSecondarySchemaStatus(int? currentVersion, int? maximumSupportedVersion)
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

    private void LogReadOnlySecondaryStatus(SecondarySchemaStatus status, int? currentVersion, int? maximumSupportedVersion)
    {
        LogLevel level = status is SecondarySchemaStatus.Unknown or SecondarySchemaStatus.Ahead
            ? LogLevel.Warning
            : LogLevel.Information;
        _logger.Log(
            level,
            "Schema write gate denied writes on a read-only geo-replication secondary. Schema status: {SchemaStatus}; current version: {CurrentVersion}; latest supported version: {LatestVersion}. Skipping schema upgrade.",
            status,
            currentVersion,
            maximumSupportedVersion);
    }
}

/// <summary>
/// Describes the state of the replicated schema on a read-only geo-replication secondary,
/// relative to the maximum version supported by the running instance.
/// </summary>
internal enum SecondarySchemaStatus
{
    /// <summary>The current schema version could not be determined.</summary>
    Unknown,

    /// <summary>The replicated schema is behind the maximum supported version.</summary>
    Behind,

    /// <summary>The replicated schema is at the maximum supported version.</summary>
    Current,

    /// <summary>The replicated schema is newer than the maximum version supported by this instance.</summary>
    Ahead,
}
