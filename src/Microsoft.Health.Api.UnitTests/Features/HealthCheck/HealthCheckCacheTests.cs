// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.HealthChecks;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

public sealed class HealthCheckCacheTests : IDisposable
{
    public HealthCheckCacheTests()
    {
        IOptions<HealthCheckServiceOptions> serviceOptions = Options.Create(new HealthCheckServiceOptions());
        serviceOptions.Value.Registrations.Add(new HealthCheckRegistration("Foo", Substitute.For<IHealthCheck>(), null, null));
        serviceOptions.Value.Registrations.Add(new HealthCheckRegistration("Bar", Substitute.For<IHealthCheck>(), null, null));

        _cache = new HealthCheckCache(serviceOptions, Options.Create(new HealthCheckCachingOptions()), NullLoggerFactory.Instance);
    }

    private readonly HealthCheckCache _cache;

    public void Dispose()
        => _cache.Dispose();

    [Theory]
    [InlineData("Foo")]
    [InlineData("Bar")]
    public void GivenKnownHealthCheckName_WhenGettingResultCache_ThenReturnResultCache(string name)
        => Assert.NotNull(_cache.GetResultCache(name));

    [Fact]
    public void GivenUnknownHealthCheckName_WhenGettingResultCache_ThenThrowKeyNotFoundException()
        => Assert.Throws<KeyNotFoundException>(() => _cache.GetResultCache("Baz"));
}
