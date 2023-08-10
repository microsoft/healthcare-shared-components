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

public class StoragePrerequisiteHealthCheckPublisher : IStoragePrerequisiteHealthCheckPublisher
{
    public HealthReport HealthReport { get; private set; }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(report, nameof(report));

        var storagePrereqs = report.Entries.Where(e => e.Value.Tags.Contains(HealthCheckTags.StoragePrerequisite.ToString())).ToDictionary(k => k.Key, v => v.Value);
        HealthReport = new HealthReport(storagePrereqs, report.TotalDuration);

        return Task.CompletedTask;
    }
}
