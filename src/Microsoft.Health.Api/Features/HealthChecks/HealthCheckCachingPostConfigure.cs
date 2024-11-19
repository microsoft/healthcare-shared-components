// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Api.Features.HealthChecks;

internal sealed class HealthCheckCachingPostConfigure : IPostConfigureOptions<HealthCheckServiceOptions>
{
    public void PostConfigure(string name, HealthCheckServiceOptions options)
    {
        EnsureArg.IsNotNull(options, nameof(options));

        HealthCheckRegistration[] list = options.Registrations.ToArray();
        options.Registrations.Clear();

        foreach (HealthCheckRegistration registration in list)
        {
            // Wrap health checks with a caching wrapper.
            string registrationName = registration.Name;
            Func<IServiceProvider, IHealthCheck> healthCheckFactory = registration.Factory;
            var newRegistration = new HealthCheckRegistration(
                registration.Name,
                provider =>
                {
                    return new CachedHealthCheck(
                        healthCheckFactory(provider),
                        registrationName,
                        provider.GetRequiredService<HealthCheckCache>());
                },
                registration.FailureStatus,
                registration.Tags,
                registration.Timeout);

            options.Registrations.Add(newRegistration);
        }
    }
}
