// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EnsureThat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Api.Features.HealthChecks
{
    public class HealthCheckConfiguration : IPostConfigureOptions<HealthCheckServiceOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public HealthCheckConfiguration(IServiceProvider serviceProvider)
        {
            EnsureArg.IsNotNull(serviceProvider, nameof(serviceProvider));

            _serviceProvider = serviceProvider;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "CachedHealthCheck lasts the lifetime of the service.")]
        public void PostConfigure(string name, HealthCheckServiceOptions options)
        {
            EnsureArg.IsNotNull(options, nameof(options));

            HealthCheckRegistration[] list = options.Registrations.ToArray();
            options.Registrations.Clear();

            ILogger<CachedHealthCheck> logger = _serviceProvider.GetRequiredService<ILogger<CachedHealthCheck>>();

            foreach (HealthCheckRegistration registration in list)
            {
                // Wrap health checks with a caching wrapper.

                var newRegistration = new HealthCheckRegistration(
                    registration.Name,
                    new CachedHealthCheck(_serviceProvider, registration.Factory, logger),
                    registration.FailureStatus,
                    registration.Tags);

                options.Registrations.Add(newRegistration);
            }
        }
    }
}
