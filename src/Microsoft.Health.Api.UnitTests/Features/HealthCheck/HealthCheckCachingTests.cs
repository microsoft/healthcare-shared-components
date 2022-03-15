// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Api.Features.HealthChecks;
using Microsoft.Health.Core.Internal;
using Microsoft.Health.Test.Utilities;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

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

        _options.Expiry = TimeSpan.FromSeconds(30);
        _options.RefreshOffset = TimeSpan.FromSeconds(28);

        CachedHealthCheck cache = CreateHealthCheck();

        // Populate cache
        result = await cache.CheckHealthAsync(_context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Start attempting to refresh a stale token
        DateTimeOffset futureTime = DateTimeOffset.UtcNow.AddSeconds(2);
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => futureTime))
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
        DateTimeOffset futureTime = DateTimeOffset.UtcNow.AddSeconds(-1);
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => futureTime))
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
            .Throw<InvalidOperationException>();

        await Assert.ThrowsAsync< InvalidOperationException>(() => CreateHealthCheck().CheckHealthAsync(_context, tokenSource.Token));

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
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

    [Theory]
    [InlineData(5, 3, 1, false)] // Fresh. Doesn't even hit the semaphore
    [InlineData(5, 3, 4, false)] // Stale
    [InlineData(5, 3, 7, true)] // Expired
    public async Task GivenTheHealthCheckCache_WhenCancellationIsRequestedBeforeHealthCheck_ThenReturnAppropriateResult(
        int expirySeconds,
        int refreshOffsetSeconds,
        int delaySeconds,
        bool throwsException)
    {
        HealthCheckResult result;
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .CheckHealthAsync(_context, tokenSource.Token)
            .Returns(Task.FromResult(HealthCheckResult.Healthy()));

        _options.Expiry = TimeSpan.FromSeconds(expirySeconds);
        _options.RefreshOffset = TimeSpan.FromSeconds(refreshOffsetSeconds);

        CachedHealthCheck cache = CreateHealthCheck();

        // Populate cache
        result = await cache.CheckHealthAsync(_context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Check health again, this time after the configured amount of time
        DateTimeOffset futureTime = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => futureTime))
        {
            tokenSource.Cancel();

            Task<HealthCheckResult> task = cache.CheckHealthAsync(_context, tokenSource.Token);
            if (throwsException)
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            }
            else
            {
                Assert.Equal(HealthStatus.Healthy, (await task).Status);
            }
        }

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
    }

    [Theory]
    [InlineData(5, 3, 4, false)] // Stale
    [InlineData(5, 3, 7, true)] // Expired
    public async Task GivenTheHealthCheckCache_WhenCancellationIsRequestedOnHealthCheck_ThenReturnAppropriateResult(
        int expirySeconds,
        int refreshOffsetSeconds,
        int delaySeconds,
        bool throwsException)
    {
        HealthCheckResult result;
        using ManualResetEventSlim startRefreshEvent = new ManualResetEventSlim(false);
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .CheckHealthAsync(_context, tokenSource.Token)
            .Returns(
                c => Task.FromResult(HealthCheckResult.Healthy()),
                c => Task.Run(() =>
                {
                    startRefreshEvent.Set();
                    tokenSource.Token.WaitHandle.WaitOne();
                    return Task.FromException<HealthCheckResult>(new OperationCanceledException(tokenSource.Token));
                }));

        _options.Expiry = TimeSpan.FromSeconds(expirySeconds);
        _options.RefreshOffset = TimeSpan.FromSeconds(refreshOffsetSeconds);

        CachedHealthCheck cache = CreateHealthCheck();

        // Populate cache
        result = await cache.CheckHealthAsync(_context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Check health again, this time after the configured amount of time.
        // We'll wait for the health check to be invoked before cancelling
        DateTimeOffset futureTime = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => futureTime))
        {
            Task<HealthCheckResult> task = cache.CheckHealthAsync(_context, tokenSource.Token);

            startRefreshEvent.Wait();
            tokenSource.Cancel();

            if (throwsException)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() => task);
            }
            else
            {
                Assert.Equal(HealthStatus.Healthy, (await task).Status);
            }
        }

        await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
    }

    [Theory]
    [InlineData(5, 3, 4, false)] // Stale
    [InlineData(5, 3, 7, true)] // Expired
    public async Task GivenTheHealthCheckCache_WhenSemaphoreUnavailable_ThenReturnAppropriateResult(
        int expirySeconds,
        int refreshOffsetSeconds,
        int delaySeconds,
        bool stuck)
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
                    return HealthCheckResult.Degraded();
                }));

        _options.Expiry = TimeSpan.FromSeconds(expirySeconds);
        _options.RefreshOffset = TimeSpan.FromSeconds(refreshOffsetSeconds);

        CachedHealthCheck cache = CreateHealthCheck();

        // Populate cache
        result = await cache.CheckHealthAsync(_context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Check health again, this time after the configured amount of time
        DateTimeOffset futureTime = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => futureTime))
        {
            // This task will enter the semaphore and refused to leave
            Task<HealthCheckResult> refreshTask = cache.CheckHealthAsync(_context, tokenSource.Token);

            // Now we'll attempt to fetch the semaphore after the semaphore is entered
            startRefreshEvent.Wait();
            Task<HealthCheckResult> blockedTask = cache.CheckHealthAsync(_context, tokenSource.Token);

            if (stuck)
            {
                // Let it continue and fetch from the newly refreshed cache
                completeRefreshEvent.Set();
                Assert.All(await Task.WhenAll(refreshTask, blockedTask), r => Assert.Equal(HealthStatus.Degraded, r.Status));
            }
            else
            {
                // Old cache is used
                Assert.Equal(HealthStatus.Healthy, (await blockedTask).Status);

                // Let the cache refresh
                completeRefreshEvent.Set();
                Assert.Equal(HealthStatus.Degraded, (await refreshTask).Status);
            }
        }

        await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
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

    [Fact]
    public async Task GivenTheHealthCheckCacheWithNoFailureCaching_WhenRefreshingWhileStale_ThenDoNotReturnNewUnhealthy()
    {
        HealthCheckResult result;
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .CheckHealthAsync(_context, tokenSource.Token)
            .Returns(
                Task.FromResult(HealthCheckResult.Degraded()),
                Task.FromResult(HealthCheckResult.Unhealthy()),
                Task.FromResult(HealthCheckResult.Healthy()));

        _options.CacheFailure = false;
        _options.Expiry = TimeSpan.FromSeconds(60);
        _options.RefreshOffset = TimeSpan.FromSeconds(50);

        CachedHealthCheck cache = CreateHealthCheck();

        // Populate cache
        result = await cache.CheckHealthAsync(_context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Degraded, result.Status);

        // Attempt to refresh
        DateTimeOffset futureTime = DateTimeOffset.UtcNow.AddSeconds(10);
        using (Mock.Property(() => ClockResolver.UtcNowFunc, () => futureTime))
        {
            // Attempt refresh but retrieve unhealthy status
            // Because we don't cache failures, and because the last status is still valid, return the old value
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Degraded, result.Status);

            // Try to get the status again (as the cache wasn't updated)
            result = await cache.CheckHealthAsync(_context, tokenSource.Token);

            await _healthCheck.Received(3).CheckHealthAsync(_context, tokenSource.Token);
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
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

        return new CachedHealthCheck(
            provider,
            s => _healthCheck,
            provider.GetRequiredService<IOptions<HealthCheckCachingOptions>>().Value,
            provider.GetRequiredService<ILogger<CachedHealthCheck>>());
    }
}
