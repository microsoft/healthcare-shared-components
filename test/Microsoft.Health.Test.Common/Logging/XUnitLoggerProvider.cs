// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Health.Test.Utilities.Logging;

/// <summary>
/// Represents an <see cref="ILoggerProvider"/> for the XUnit test framework.
/// </summary>
public sealed class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _outputHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="XUnitLoggerProvider"/>
    /// with the specified <see cref="IMessageSink"/>.
    /// </summary>
    /// <param name="sink">A sink for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sink"/> is <see langword="null"/>.</exception>
    public XUnitLoggerProvider(IMessageSink sink)
        : this(new TestOutputHelperSink(sink))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="XUnitLoggerProvider"/>
    /// with the specified <see cref="ITestOutputHelper"/>.
    /// </summary>
    /// <param name="outputHelper">A console-like outputter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="outputHelper"/> is <see langword="null"/>.</exception>
    public XUnitLoggerProvider(ITestOutputHelper outputHelper)
        => _outputHelper = EnsureArg.IsNotNull(outputHelper, nameof(outputHelper));

    /// <inheritdoc cref="ILoggerProvider.CreateLogger(string)" />
    public ILogger CreateLogger(string categoryName)
        => new XUnitLogger(categoryName, _outputHelper);

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    { }

    private sealed class TestOutputHelperSink : ITestOutputHelper
    {
        private readonly IMessageSink _sink;

        public TestOutputHelperSink(IMessageSink sink)
            => _sink = EnsureArg.IsNotNull(sink, nameof(sink));

        public void WriteLine(string message)
            => _sink.OnMessage(new DiagnosticMessage(message));

        public void WriteLine(string format, params object[] args)
            => _sink.OnMessage(new DiagnosticMessage(format, args));
    }
}
