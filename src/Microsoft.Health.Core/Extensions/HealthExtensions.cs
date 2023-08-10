// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Extensions;

public static class HealthExtensions
{
    public static HealthReportEntry FindLowestHealthReportEntry(this HealthReport healthReport)
    {
        EnsureArg.IsNotNull(healthReport, nameof(healthReport));

        HealthReportEntry reportEntryWithLowestStatus = healthReport.Entries.First().Value;
        foreach (var entry in healthReport.Entries.Values)
        {
            if (entry.Status == HealthStatus.Unhealthy)
            {
                // this is the lowest status, stop looking
                reportEntryWithLowestStatus = entry;
                break;
            }
            else if (entry.Status < reportEntryWithLowestStatus.Status)
            {
                reportEntryWithLowestStatus = entry;
            }
        }

        return reportEntryWithLowestStatus;
    }
}
