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
        public async Task GivenTheHealthCheckCache_WhenCacheFresh_ThenDoNotRefresh()
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
        public async Task GivenTheHealthCheckCache_WhenCacheStale_ThenOnlyOneRefreshes()
        {
            HealthCheckResult result;
            using ManualResetEventSlim startRefreshEvent = new ManualResetEventSlim(false);
            using ManualResetEventSlim completeRefreshEvent = new ManualResetEventSlim(false);
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .CheckHealthAsync(_context, tokenSource.Token)
                .Returns(
                    c => Task.FromResult(HealthCheckResult.Healthy()),
                    c => Task.Run(() =>
                    {
                        startRefreshEvent.Set();
                        completeRefreshEvent.Wait(); // Wait for event
                        return HealthCheckResult.Healthy();
                    }));

            _options.Expiry = TimeSpan.FromSeconds(10);
            _options.RefreshOffset = TimeSpan.FromSeconds(5);

            CachedHealthCheck cache = CreateHealthCheck();

            // Populate cache
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);

            // Start attempting to refresh a stale token
            using (Mock.Property(() => ClockResolver.UtcNowFunc, () => DateTimeOffset.UtcNow.AddSeconds(5)))
            {
                Task<HealthCheckResult> semaphoreConsumerTask = cache.CheckHealthAsync(_context, tokenSource.Token);

                // Wait until the semaphore has been entered
                startRefreshEvent.Wait();
                await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);

                // Simultaneously attempt, and because the semaphore cannot be entered the cached value is returned
                result = await cache.CheckHealthAsync(_context, tokenSource.Token);

                await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
                Assert.Equal(HealthStatus.Healthy, result.Status);

                // Complete the previous task
                completeRefreshEvent.Set();
                result = await semaphoreConsumerTask;
            }

            await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenCacheExpired_ThenCacheRefreshed()
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
        public async Task GivenTheHealthCheckCache_WhenHealthCheckCanceled_ThenTheResultIsWrittenCorrectly()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .When(c => c.CheckHealthAsync(_context, tokenSource.Token))
                .Throw(new OperationCanceledException(tokenSource.Token));

            await Assert.ThrowsAsync<OperationCanceledException>(() => CreateHealthCheck().CheckHealthAsync(_context, tokenSource.Token));
        }

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenCancellationIsRequestedAndFresh_ThenReturnLastHealthCheckResult()
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

            // Check again and confirm we only called CheckHealthAsync once
            tokenSource.Cancel();
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenCancellationIsRequestedAndStale_ThenReturnLastHealthCheckResult()
        {
            HealthCheckResult result;
            using ManualResetEventSlim startRefreshEvent = new ManualResetEventSlim(false);
            using ManualResetEventSlim completeRefreshEvent = new ManualResetEventSlim(false);
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .CheckHealthAsync(_context, tokenSource.Token)
                .Returns(
                    c => Task.FromResult(HealthCheckResult.Healthy()),
                    c => Task.Run(() =>
                    {
                        startRefreshEvent.Set();
                        completeRefreshEvent.Wait(); // Wait for event
                        return HealthCheckResult.Healthy();
                    }));

            _options.Expiry = TimeSpan.FromSeconds(10);
            _options.RefreshOffset = TimeSpan.FromSeconds(5);

            CachedHealthCheck cache = CreateHealthCheck();

            // Populate cache
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);

            // Start attempting to refresh a stale token
            using (Mock.Property(() => ClockResolver.UtcNowFunc, () => DateTimeOffset.UtcNow.AddSeconds(5)))
            {
                Task<HealthCheckResult> semaphoreConsumerTask = cache.CheckHealthAsync(_context, tokenSource.Token);

                // Wait until the semaphore has been entered
                startRefreshEvent.Wait();

                // Cancel and witness the thrown exception on the next thread that cannot enter the semaphore
                // (Note the "consumer task" will ignore this signal)
                tokenSource.Cancel();
                await Assert.ThrowsAsync<TaskCanceledException>(() => cache.CheckHealthAsync(_context, tokenSource.Token));

                // Complete the previous task
                completeRefreshEvent.Set();
                result = await semaphoreConsumerTask;
            }

            await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task GivenTheHealthCheckCache_WhenCancellationIsRequestedAndExpired_ThenThrowException()
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .CheckHealthAsync(_context, tokenSource.Token)
                .Returns(Task.FromResult(HealthCheckResult.Healthy()));

            _options.Expiry = TimeSpan.FromSeconds(1);

            CachedHealthCheck cache = CreateHealthCheck();

            // Populate cache
            HealthCheckResult result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);

            // Attempt to fetch a health check with a canceled token after the cache expired
            tokenSource.Cancel();
            using (Mock.Property(() => ClockResolver.UtcNowFunc, () => DateTimeOffset.UtcNow.AddSeconds(1)))
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => cache.CheckHealthAsync(_context, tokenSource.Token));
            }

            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GivenTheHealthCheckCache_WhenCachingFailure_ThenStoreBasedOnConfig(bool cacheFailure)
        {
            HealthCheckResult result;
            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            _healthCheck
                .CheckHealthAsync(_context, tokenSource.Token)
                .Returns(Task.FromResult(HealthCheckResult.Unhealthy()));

            _options.CacheFailure = cacheFailure;
            _options.Expiry = TimeSpan.FromDays(1);

            CachedHealthCheck cache = CreateHealthCheck();

            // Populate cache (if caching)
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);

            // Check again
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            await _healthCheck.Received(cacheFailure ? 1 : 2).CheckHealthAsync(_context, tokenSource.Token);
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
