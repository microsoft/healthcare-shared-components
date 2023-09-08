// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Features.Health;

internal static class HealthReportExtensions
{
    public static HealthStatusReason GetHighestSeverityReason(this HealthReport healthReport, HealthStatusReason defaultReason)
    {
        HealthStatusReason worstReason = defaultReason;

        foreach (var entry in healthReport.Entries)
        {
            if (entry.Value.Data.TryGetValue("Reason", out object reason))
            {
                HealthStatusReason healthStatusReason = Enum.Parse<HealthStatusReason>(reason.ToString());

                if (healthStatusReason > worstReason)
                {
                    worstReason = healthStatusReason;
                }
            }
        }

        return worstReason;
    }
}
