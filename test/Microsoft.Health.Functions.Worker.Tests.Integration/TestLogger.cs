// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Functions.Worker.Tests.Integration;

internal sealed class TestLogger(string _category, TestContext _context) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel)
        => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _context.WriteLine("{0:O} {1}: {2}[{3}] => {4}", DateTimeOffset.UtcNow, logLevel, _category, eventId, formatter(state, exception));
}

internal sealed class TestLoggerProvider(TestContext _context) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new TestLogger(categoryName, _context);

    public void Dispose()
    { }
}
