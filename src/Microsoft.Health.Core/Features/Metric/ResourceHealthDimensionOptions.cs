// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Core.Features.Metric;

/// <summary>
/// Defines required dimensions for reporting metrics to Geneva Health
/// </summary>
public class ResourceHealthDimensionOptions
{
    public const string SectionName = "ResourceHealthDimension";

    public string ResourceType { get; set; }

    public string ArmGeoLocation { get; set; }

    public string ArmResourceId { get; set; }
}
