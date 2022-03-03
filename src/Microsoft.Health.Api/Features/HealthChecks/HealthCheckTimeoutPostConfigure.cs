// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Microsoft.Health.Api.Features.HealthChecks
{
    internal sealed class HealthCheckTimeoutPostConfigure : IPostConfigureOptions<HealthCheckServiceOptions>
    {
        private readonly TimeSpan _timeout;

        public HealthCheckTimeoutPostConfigure(TimeSpan timeout)
            => _timeout = EnsureArg.IsGte(timeout, TimeSpan.Zero, nameof(timeout));

        public void PostConfigure(string name, HealthCheckServiceOptions options)
        {
            EnsureArg.IsNotNull(options, nameof(options));

            foreach (HealthCheckRegistration registration in options.Registrations)
            {
                registration.Timeout = _timeout;
            }
        }
    }
}
