// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DurableTask.Core;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Microsoft.Health.Operations.Functions.Management;

/// <summary>
/// Represents the settings for configuring how to clean up orchestration instance metadata.
/// </summary>
public class PurgeHistoryOptions
{
    /// <summary>
    /// The default section name for <see cref="PurgeHistoryOptions"/> in a configuration.
    /// </summary>
    public const string SectionName = "PurgeHistory";

    /// <summary>
    /// Gets or sets the collection of statuses which should be considered for deletion.
    /// </summary>
    /// <value>A set of at least one <see cref="OrchestrationRuntimeStatus"/>.</value>
    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<OrchestrationRuntimeStatus>? Statuses { get; set; } = new OrchestrationRuntimeStatus[] { OrchestrationRuntimeStatus.Completed };

    /// <summary>
    /// Gets or sets the minimum amount of time from when the orchestration was created
    /// that should be considered for deletion.
    /// </summary>
    /// <value>A positive number greater than zero.</value>
    [Range(1, 365)]
    public int MinimumAgeDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the cron expression that indicates how frequently orchestration
    /// instance metadata should be deleted.
    /// </summary>
    /// <value>A value cron expression</value>
    [Required]
    public string? Frequency { get; set; }

    /// <summary>
    ///  Gets or sets the collection of orchestration names which should be skipped for cleanup
    /// </summary>
    /// <value>A set of one or more <see cref="string"/></value>
    public IReadOnlyCollection<string>? InstancesToSkipPurging { get; set; }
}
