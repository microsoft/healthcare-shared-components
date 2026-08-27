// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// Default <see cref="ISchemaMetrics"/> implementation backed by a <see cref="Meter"/>. Downstream hosts
/// subscribe to the meter (by <see cref="MeterName"/>) and attach an exporter to collect the metrics.
/// </summary>
internal sealed class SchemaMetrics : ISchemaMetrics, IDisposable
{
    /// <summary>
    /// The name of the <see cref="Meter"/> that publishes the schema metrics.
    /// </summary>
    public const string MeterName = "Microsoft.Health.SqlServer";

    private readonly Meter _meter;
    private readonly Counter<long> _schemaBehindCounter;

    public SchemaMetrics()
    {
        _meter = new Meter(MeterName);
        _schemaBehindCounter = _meter.CreateCounter<long>(
            "healthcare_sqlserver_schema_behind",
            unit: null,
            description: "The number of times a database was found running a schema version behind the latest supported version.");
    }

    public void SchemaBehind(string databaseName, int schemaVersion, string region)
    {
        var tags = new TagList
        {
            { "database_name", databaseName },
            { "schema_version", schemaVersion },
            { "region", region },
        };

        _schemaBehindCounter.Add(1, tags);
    }

    public void Dispose() => _meter.Dispose();
}
