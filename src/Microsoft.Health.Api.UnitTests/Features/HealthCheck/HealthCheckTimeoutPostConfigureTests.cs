// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

[TestClass]
public class HealthCheckTimeoutPostConfigureTests
{
    [TestMethod]
    public void GivenHealthChecks_WhenOverridingTimeout_ThenReplaceTimeout()
    {
        IServiceProvider provider = new ServiceCollection()
            .AddHealthChecks()
            .AddCheck("Foo", Substitute.For<IHealthCheck>())
            .AddCheck("Bar", Substitute.For<IHealthCheck>(), timeout: TimeSpan.FromMinutes(1))
            .AddCheck("Baz", Substitute.For<IHealthCheck>(), timeout: TimeSpan.FromSeconds(30))
            .Services
            .ConfigureHealthCheckTimeout(TimeSpan.FromSeconds(30))
            .BuildServiceProvider();

        IOptions<HealthCheckServiceOptions> options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
        Assert.HasCount(3, options.Value.Registrations);

        foreach (HealthCheckRegistration r in options.Value.Registrations)
            Assert.AreEqual(TimeSpan.FromSeconds(30), r.Timeout);
    }
}
