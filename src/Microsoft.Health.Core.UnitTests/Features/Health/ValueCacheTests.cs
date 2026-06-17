// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Health.Core.Features.Health;
using Xunit;

namespace Microsoft.Health.Core.UnitTests.Features.Health;

public class ValueCacheTests
{
    [Fact]
    public async Task GivenNoExpiry_WhenSetAndGet_ThenReturnsValue()
    {
        ValueCache<string> cache = new ValueCache<string>();

        cache.Set("hello");

        string result = await cache.GetAsync();

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task GivenNoExpiry_WhenLongDelayBetweenSetAndGet_ThenStillReturnsValue()
    {
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ValueCache<string> cache = new ValueCache<string>(Timeout.InfiniteTimeSpan, timeProvider);

        cache.Set("hello");
        timeProvider.Advance(TimeSpan.FromDays(7));

        string result = await cache.GetAsync();

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task GivenExpiry_WhenValueIsFresh_ThenReturnsValue()
    {
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ValueCache<string> cache = new ValueCache<string>(TimeSpan.FromMinutes(5), timeProvider);

        cache.Set("hello");
        timeProvider.Advance(TimeSpan.FromMinutes(2));

        string result = await cache.GetAsync();

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task GivenExpiry_WhenValueIsExactlyAtExpiry_ThenReturnsValue()
    {
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ValueCache<string> cache = new ValueCache<string>(TimeSpan.FromMinutes(5), timeProvider);

        cache.Set("hello");
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        string result = await cache.GetAsync();

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task GivenExpiry_WhenValueIsStale_ThenReturnsNull()
    {
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ValueCache<string> cache = new ValueCache<string>(TimeSpan.FromMinutes(5), timeProvider);

        cache.Set("hello");
        timeProvider.Advance(TimeSpan.FromMinutes(5).Add(TimeSpan.FromTicks(1)));

        string result = await cache.GetAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GivenExpiry_WhenSetIsCalledAgainBeforeExpiry_ThenReturnsLatestValue()
    {
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ValueCache<string> cache = new ValueCache<string>(TimeSpan.FromMinutes(5), timeProvider);

        cache.Set("first");
        timeProvider.Advance(TimeSpan.FromMinutes(4));
        cache.Set("second");
        timeProvider.Advance(TimeSpan.FromMinutes(4));

        string result = await cache.GetAsync();

        Assert.Equal("second", result);
    }

    [Fact]
    public async Task GivenExpiry_WhenSetIsCalledAfterExpiry_ThenFreshAgain()
    {
        FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        ValueCache<string> cache = new ValueCache<string>(TimeSpan.FromMinutes(5), timeProvider);

        cache.Set("first");
        timeProvider.Advance(TimeSpan.FromMinutes(10));

        Assert.Null(await cache.GetAsync());

        cache.Set("second");

        Assert.Equal("second", await cache.GetAsync());
    }

    [Fact]
    public async Task GivenNoSet_WhenGetWithCancellation_ThenThrows()
    {
        ValueCache<string> cache = new ValueCache<string>();
        using CancellationTokenSource cts = new();

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetAsync(cts.Token));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenNonPositiveExpiry_WhenConstructed_ThenThrows(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ValueCache<string>(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void GivenInfiniteExpiry_WhenConstructed_ThenSucceeds()
    {
        _ = new ValueCache<string>(Timeout.InfiniteTimeSpan);
    }

    [Fact]
    public async Task GivenStaleCache_WhenGetAsync_ThenLogsWarning()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        TestLogger<ValueCache<string>> logger = new TestLogger<ValueCache<string>>();
        ValueCache<string> cache = new ValueCache<string>(TimeSpan.FromMinutes(5), timeProvider, logger);

        cache.Set("v1");
        timeProvider.Advance(TimeSpan.FromMinutes(6));

        string result = await cache.GetAsync();

        Assert.Null(result);
        LogEntry entry = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("stale", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("String", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GivenFreshCache_WhenGetAsync_ThenDoesNotLog()
    {
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        TestLogger<ValueCache<string>> logger = new TestLogger<ValueCache<string>>();
        ValueCache<string> cache = new ValueCache<string>(TimeSpan.FromMinutes(5), timeProvider, logger);

        cache.Set("v1");
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        string result = await cache.GetAsync();

        Assert.Equal("v1", result);
        Assert.Empty(logger.Entries);
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class TestLogger<T> : ILogger<T>
    {
        public System.Collections.Generic.List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
