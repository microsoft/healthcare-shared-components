// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Health.Core.Features.Health;

public class HealthCheckPublisher : IHealthCheckPublisher
{
    public static HealthReport Latest { get; set; }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(report, nameof(report));

        Latest = report;

        return Task.CompletedTask;
    }
}
