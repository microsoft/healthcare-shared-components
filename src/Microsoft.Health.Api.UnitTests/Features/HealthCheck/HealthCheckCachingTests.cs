// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Health.Api.Features.HealthChecks;
using Microsoft.Health.Core.Internal;
using Microsoft.Health.Test.Utilities;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck
{
    public class HealthCheckCachingTests
    {
        private readonly HealthCheckContext _context = new HealthCheckContext();
        private readonly IHealthCheck _healthCheck = Substitute.For<IHealthCheck>();
        private readonly DateTimeOffset _currentTime = DateTimeOffset.UtcNow;
        private readonly HealthCheckCachingOptions _options = new HealthCheckCachingOptions();

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenCallingWithMultipleRequests_ThenOnlyOneResultShouldBeExecuted()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .CheckHealthAsync(_context, tokenSource.Token)
                .Returns(Task.FromResult(HealthCheckResult.Healthy()));

            _options.Expiry = TimeSpan.FromDays(1);

            CachedHealthCheck cache = CreateHealthCheck();

            HealthCheckResult[] actual = await Task.WhenAll(
                cache.CheckHealthAsync(_context, tokenSource.Token),
                cache.CheckHealthAsync(_context, tokenSource.Token),
                cache.CheckHealthAsync(_context, tokenSource.Token),
                cache.CheckHealthAsync(_context, tokenSource.Token));

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.All(actual, x => Assert.Equal(HealthStatus.Healthy, x.Status));
        }

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenHealthCheckThrows_ThenTheResultIsWrittenCorrectly()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .When(c => c.CheckHealthAsync(_context, tokenSource.Token))
                .Throw<Exception>();

            HealthCheckResult result = await CreateHealthCheck().CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
        }

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenMoreThan1SecondApart_ThenSecondRequestGetsFreshResults()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .CheckHealthAsync(_context, tokenSource.Token)
                .Returns(Task.FromResult(HealthCheckResult.Healthy()));

            _options.Expiry = TimeSpan.FromSeconds(1);

            CachedHealthCheck cache = CreateHealthCheck();

            // Mocks the time a second ago so we can call the middleware in the past
            using (Mock.Property(() => ClockResolver.UtcNowFunc, () => DateTimeOffset.UtcNow.AddSeconds(-1)))
            {
                Assert.All(
                    await Task.WhenAll(
                        cache.CheckHealthAsync(_context, tokenSource.Token),
                        cache.CheckHealthAsync(_context, tokenSource.Token)),
                    x => Assert.Equal(HealthStatus.Healthy, x.Status));
            }

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);

            // Call the middleware again to ensure we get new results
            Assert.Equal(HealthStatus.Healthy, (await cache.CheckHealthAsync(_context, tokenSource.Token)).Status);

            await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
        }

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenCancellationIsRequested_ThenWeDoNotThrowAndReturnLastHealthCheckResult()
        {
            HealthCheckResult result;
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .CheckHealthAsync(_context, tokenSource.Token)
                .Returns(Task.FromResult(HealthCheckResult.Healthy()));

            _options.Expiry = TimeSpan.FromDays(1);

            CachedHealthCheck cache = CreateHealthCheck();

            // Populate cache
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);

            // Check again
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            // Confirm we only called CheckHealthAsync once.
            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        private CachedHealthCheck CreateHealthCheck()
        {
            IServiceProvider provider = new ServiceCollection()
                .AddLogging()
                .AddSingleton(() => _currentTime)
                .Configure<HealthCheckCachingOptions>(x =>
                {
                    x.CacheFailure = _options.CacheFailure;
                    x.Expiry = _options.Expiry;
                    x.RefreshOffset = _options.RefreshOffset;
                })
                .BuildServiceProvider();

            return new CachedHealthCheck(provider, s => _healthCheck);
        }
    }
}
