// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.SqlServer.Configs;

/// <summary>
/// Represents the settings used to configure the schema for a given database.
/// </summary>
public class SqlServerSchemaOptions
{
    /// <summary>
    /// Gets or sets whether updates to the schema should be applied automatically.
    /// </summary>
    /// <value><c>true</c> if automatic updates are enabled; otherwise, <c>false</c>.</value>
    public bool AutomaticUpdatesEnabled { get; set; }

    /// <summary>
    /// Gets or sets the polling frequency for the schema updates.
    /// </summary>
    /// <value>The time between polls. The default is <c>60</c> seconds.</value>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00", ConvertValueInInvariantCulture = true)]
    public TimeSpan JobPollingFrequency { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the amount of time after which an expired instance record may be deleted.
    /// </summary>
    /// <value>The expiration time. The default is <c>2</c> minutes.</value>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00", ConvertValueInInvariantCulture = true)]
    public TimeSpan InstanceRecordExpirationTime { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the schema version at which the service should terminate.
    /// </summary>
    /// <value>
    /// The schema version that, once reached, instructs the service to terminate;
    /// otherwise, <see langword="null"/> if the service should not terminate on schema updates.
    /// </value>
    [Range(1, int.MaxValue, ConvertValueInInvariantCulture = true)]
    public int? CompletionSchemaVersion { get; set; }
}
