// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.SqlServer.Features.Schema;

/// <summary>
/// Represents the settings for the SQL database's application version.
/// </summary>
public class SqlSchemaVersionOptions
{
    /// <summary>
    /// Gets or sets the minimum supported version.
    /// </summary>
    /// <value>The positive minimum supported version number.</value>
    [Range(1, int.MaxValue)]
    public int MinimumVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum supported version.
    /// </summary>
    /// <value>The positive maximum supported version number.</value>
    [Range(1, int.MaxValue)]
    public int MaximumVersion { get; set; } = 1;
}
