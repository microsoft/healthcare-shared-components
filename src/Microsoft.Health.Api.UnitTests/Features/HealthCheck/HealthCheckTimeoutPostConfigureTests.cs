// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck
{
    public class HealthCheckTimeoutPostConfigureTests
    {
        [Fact]
        public void GivenHealthChecks_WhenOverridingTimeout_ThenReplaceTimeout()
        {
            IServiceProvider provider = new ServiceCollection()
                .AddHealthChecks()
                .AddCheck<ExampleHealthCheck>("Foo")
                .AddCheck<ExampleHealthCheck>("Bar", timeout: TimeSpan.FromMinutes(1))
                .AddCheck<ExampleHealthCheck>("Baz", timeout: TimeSpan.FromSeconds(30))
                .Services
                .ConfigureHealthCheckTimeout(TimeSpan.FromSeconds(30))
                .BuildServiceProvider();

            IOptions<HealthCheckServiceOptions> options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
            Assert.Equal(3, options.Value.Registrations.Count);
            Assert.All(options.Value.Registrations, r => Assert.Equal(TimeSpan.FromSeconds(30), r.Timeout));
        }

        private sealed class ExampleHealthCheck : IHealthCheck
        {
            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }
    }
}
