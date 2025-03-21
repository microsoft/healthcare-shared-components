// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Health.Api.Features.HealthChecks;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Api.UnitTests.Features.HealthCheck;

public class HealthCheckResultCacheTests
{
    private readonly HealthCheckContext _context = new HealthCheckContext();
    private readonly IHealthCheck _healthCheck = Substitute.For<IHealthCheck>();
    private readonly HealthCheckCachingOptions _options = new HealthCheckCachingOptions();

    [Theory]
    [InlineData(HealthStatus.Unhealthy, HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded, HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded, HealthStatus.Degraded)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Degraded)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Healthy)]
    public async Task GivenTheHealthCheckCache_WhenCacheFresh_ThenDoNotRefresh(HealthStatus status, HealthStatus minimumCachedStatus)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .CheckHealthAsync(_context, tokenSource.Token)
            .Returns(Task.FromResult(new HealthCheckResult(status)));

        _options.Expiry = TimeSpan.FromDays(1);
        _options.MaxRefreshThreads = 1;
        _options.MinimumCachedHealthStatus = minimumCachedStatus;

        using HealthCheckResultCache cache = CreateHealthCheck();

        HealthCheckResult[] actual = await Task.WhenAll(
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token),
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token),
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token),
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token));

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.All(actual, x => Assert.Equal(status, x.Status));
    }

    [Theory]
    [InlineData(HealthStatus.Unhealthy, HealthStatus.Degraded)]
    [InlineData(HealthStatus.Unhealthy, HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded, HealthStatus.Healthy)]
    public async Task GivenTheHealthCheckCache_WhenCacheFreshButUncacheableStatus_ThenAlwaysRefresh(HealthStatus status, HealthStatus minimumCachedStatus)
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .CheckHealthAsync(_context, tokenSource.Token)
            .Returns(Task.FromResult(new HealthCheckResult(status)));

        _options.Expiry = TimeSpan.FromDays(1);
        _options.MaxRefreshThreads = 1;
        _options.MinimumCachedHealthStatus = minimumCachedStatus;

        using HealthCheckResultCache cache = CreateHealthCheck();

        HealthCheckResult[] actual = await Task.WhenAll(
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token),
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token),
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token),
            cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token));

        await _healthCheck.Received(4).CheckHealthAsync(_context, tokenSource.Token);
        Assert.All(actual, x => Assert.Equal(status, x.Status));
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
        _options.MaxRefreshThreads = 1;

        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        using HealthCheckResultCache cache = CreateHealthCheck(timeProvider);

        // Populate cache
        result = await cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Start attempting to refresh a stale token
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Task<HealthCheckResult> semaphoreConsumerTask = cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        // Wait until the semaphore has been entered
        startRefreshEvent.Wait();
        await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);

        // Simultaneously attempt, and because the semaphore cannot be entered the cached value is returned
        result = await cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Complete the previous task
        completeRefreshEvent.Set();
        result = await semaphoreConsumerTask;

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
        _options.MaxRefreshThreads = 1;

        // Mocks the time a second ago so we can call the middleware in the past
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow.AddSeconds(-1));
        using HealthCheckResultCache cache = CreateHealthCheck(timeProvider);

        Assert.All(
            await Task.WhenAll(
                cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token),
                cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token)),
            x => Assert.Equal(HealthStatus.Healthy, x.Status));

        timeProvider.Advance(TimeSpan.FromSeconds(1));

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);

        // Call the middleware again to ensure we get new results
        Assert.Equal(HealthStatus.Healthy, (await cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token)).Status);

        await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
    }

    [Fact]
    public async Task GivenExpiredHealthCheckCache_WhenHealthCheckThrows_ThenExceptionIsThrown()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .When(c => c.CheckHealthAsync(_context, tokenSource.Token))
            .Throw<IOException>();

        await Assert.ThrowsAsync<IOException>(() => CreateHealthCheck().CheckHealthAsync(_healthCheck, _context, tokenSource.Token));
    }

    [Fact]
    public async Task GivenExpiredHealthCheckCache_WhenHealthCheckCanceled_ThenExceptionIsThrown()
    {
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .When(c => c.CheckHealthAsync(_context, tokenSource.Token))
            .Throw(new OperationCanceledException(tokenSource.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateHealthCheck().CheckHealthAsync(_healthCheck, _context, tokenSource.Token));
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

        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        using HealthCheckResultCache cache = CreateHealthCheck(timeProvider);

        // Populate cache
        result = await cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Check health again, this time after the configured amount of time
        timeProvider.Advance(TimeSpan.FromSeconds(delaySeconds));
        await tokenSource.CancelAsync();

        Task<HealthCheckResult> task = cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);
        if (throwsException)
            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        else
            Assert.Equal(HealthStatus.Healthy, (await task).Status);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
    }

    [Theory]
    [InlineData(5, 3, 4, false, false)] // Stale - Exception
    [InlineData(5, 3, 4, true, false)] // Stale - Cancellation
    [InlineData(5, 3, 7, false, true)] // Expired - Exception
    [InlineData(5, 3, 7, true, true)] // Expired - Cancellation
    public async Task GivenTheHealthCheckCache_WhenExceptionThrownOnHealthCheck_ThenReturnAppropriateResult(
        int expirySeconds,
        int refreshOffsetSeconds,
        int delaySeconds,
        bool isCancellation,
        bool throwsException)
    {
        HealthCheckResult result;
        using ManualResetEventSlim startRefreshEvent = new ManualResetEventSlim(false);
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        Exception exception = isCancellation ? new OperationCanceledException(tokenSource.Token) : new IOException();
        _healthCheck
            .CheckHealthAsync(_context, tokenSource.Token)
            .Returns(
                c => Task.FromResult(HealthCheckResult.Healthy()),
                c => Task.Run(() =>
                {
                    startRefreshEvent.Set();
                    tokenSource.Token.WaitHandle.WaitOne();
                    return Task.FromException<HealthCheckResult>(exception);
                }));

        _options.Expiry = TimeSpan.FromSeconds(expirySeconds);
        _options.RefreshOffset = TimeSpan.FromSeconds(refreshOffsetSeconds);
        _options.MaxRefreshThreads = 1;

        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        using HealthCheckResultCache cache = CreateHealthCheck(timeProvider);

        // Populate cache
        result = await cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Check health again, this time after the configured amount of time.
        // We'll wait for the health check to be invoked before cancelling
        timeProvider.Advance(TimeSpan.FromSeconds(delaySeconds));
        Task<HealthCheckResult> task = cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        startRefreshEvent.Wait();
        await tokenSource.CancelAsync();

        if (throwsException)
            await Assert.ThrowsAsync(isCancellation ? typeof(OperationCanceledException) : typeof(IOException), () => task);
        else
            Assert.Equal(HealthStatus.Healthy, (await task).Status);

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
        _options.MaxRefreshThreads = 1;

        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        using HealthCheckResultCache cache = CreateHealthCheck(timeProvider);

        // Populate cache
        result = await cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        await _healthCheck.Received(1).CheckHealthAsync(_context, tokenSource.Token);
        Assert.Equal(HealthStatus.Healthy, result.Status);

        // Check health again, this time after the configured amount of time
        timeProvider.Advance(TimeSpan.FromSeconds(delaySeconds));

        // This task will enter the semaphore and refuse to leave
        Task<HealthCheckResult> refreshTask = cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        // Now we'll attempt to fetch the semaphore after the semaphore is entered
        startRefreshEvent.Wait();
        Task<HealthCheckResult> blockedTask = cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

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

        await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded, HealthStatus.Healthy, HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded, HealthStatus.Unhealthy, HealthStatus.Degraded)]
    public async Task GivenTheHealthCheckCache_WhenMultipleThreadsRefreshing_ThenGracefullyHandleOverlap(
        HealthStatus first,
        HealthStatus second,
        HealthStatus expected)
    {
        using ManualResetEventSlim startEvent1 = new ManualResetEventSlim(false);
        using ManualResetEventSlim completeEvent1 = new ManualResetEventSlim(false);
        using ManualResetEventSlim startEvent2 = new ManualResetEventSlim(false);
        using ManualResetEventSlim completeEvent2 = new ManualResetEventSlim(false);
        using CancellationTokenSource tokenSource = new CancellationTokenSource();

        _healthCheck
            .CheckHealthAsync(_context, tokenSource.Token)
            .Returns(
                c => Task.Run(() =>
                {
                    startEvent1.Set();
                    completeEvent1.Wait(); // Wait for event
                    return new HealthCheckResult(first);
                }),
                c => Task.Run(() =>
                {
                    startEvent2.Set();
                    completeEvent2.Wait(); // Wait for event
                    return new HealthCheckResult(second);
                }));

        _options.Expiry = TimeSpan.FromSeconds(30);
        _options.MaxRefreshThreads = 2;

        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        using HealthCheckResultCache cache = CreateHealthCheck(timeProvider);

        // Start two threads who will concurrently attempt to populate the cache
        // These tasks will enter the semaphore and refuse to leave
        Task<HealthCheckResult> refreshTask1 = cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);
        Task<HealthCheckResult> refreshTask2 = cache.CheckHealthAsync(_healthCheck, _context, tokenSource.Token);

        // Wait until the semaphore has been entered by both threads
        startEvent1.Wait();
        startEvent2.Wait();

        await _healthCheck.Received(2).CheckHealthAsync(_context, tokenSource.Token);

        // Allow the first task to complete to update the cache
        completeEvent1.Set();
        Assert.Equal(first, (await refreshTask1).Status);

        // Allow the second task to complete and see the cache has already been updated
        completeEvent2.Set();
        Assert.Equal(expected, (await refreshTask2).Status);
    }

    private HealthCheckResultCache CreateHealthCheck(TimeProvider timeProvider = null)
        => new HealthCheckResultCache(timeProvider ?? TimeProvider.System, Options.Create(_options), NullLoggerFactory.Instance);
}
