// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Health.Api.Features.HealthChecks
{
    public class HealthCheckCachingOptions
    {
        [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
        public TimeSpan Expiry { get; set; } = TimeSpan.FromSeconds(1);

        [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00", ConvertValueInInvariantCulture = true, ParseLimitsInInvariantCulture = true)]
        public TimeSpan RefreshOffset { get; set; } = TimeSpan.Zero;

        public bool CacheFailure { get; set; } = true;
    }
}
