// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Features.Health;

public class HealthCheckPublisher : IHealthCheckPublisher
{
    private readonly IStoragePrerequisiteHealthReport _storagePrerequisiteHealthReport;

    public HealthCheckPublisher(IStoragePrerequisiteHealthReport healthReport)
    {
        _storagePrerequisiteHealthReport = EnsureArg.IsNotNull(healthReport, nameof(healthReport));
    }
    public HealthReport HealthReport { get; private set; }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(report, nameof(report));

        var storagePrereqs = report.Entries.Where(e => e.Value.Tags.Contains(HealthCheckTags.StoragePrerequisite.ToString())).ToDictionary(k => k.Key, v => v.Value);
        _storagePrerequisiteHealthReport.HealthReport = new HealthReport(storagePrereqs, report.TotalDuration);

        return Task.CompletedTask;
    }
}
