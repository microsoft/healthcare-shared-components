// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.HealthChecks;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck
{
    public class HealthCheckCachingPostConfigureTests
    {
        [Fact]
        public void GivenHealthChecks_WhenOverridingTimeout_ThenReplaceTimeout()
        {
            IServiceProvider provider = new ServiceCollection()
                .ConfigureHealthCheckCache(o => o.Timeout = TimeSpan.FromMinutes(5))
                .AddTransient<IPostConfigureOptions<HealthCheckServiceOptions>, HealthCheckCachingPostConfigure>()
                .AddLogging()
                .AddHealthChecks()
                .AddCheck("Foo", Substitute.For<IHealthCheck>())
                .AddCheck("Bar", Substitute.For<IHealthCheck>(), timeout: TimeSpan.FromMinutes(1))
                .AddCheck("Baz", Substitute.For<IHealthCheck>(), timeout: TimeSpan.FromSeconds(30))
                .Services
                .BuildServiceProvider();

            IOptions<HealthCheckServiceOptions> options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
            Assert.Equal(3, options.Value.Registrations.Count);
            Assert.All(options.Value.Registrations, r => Assert.Equal(TimeSpan.FromMinutes(5), r.Timeout));
        }
    }
}
