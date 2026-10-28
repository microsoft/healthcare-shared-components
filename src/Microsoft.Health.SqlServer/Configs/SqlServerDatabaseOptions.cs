// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

#nullable enable

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.SqlServer.Configs;

/// <summary>
/// Represents the settings for a given database.
/// </summary>
public class SqlServerDatabaseOptions
{
    /// <summary>
    /// Gets or sets the SQL connection string for the database.
    /// </summary>
    /// <value>The connection string value.</value>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = default!;
}
