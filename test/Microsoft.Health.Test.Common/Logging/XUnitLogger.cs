// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Health.Test.Utilities.Logging;

internal sealed class XUnitLogger : ILogger
{
    private readonly string _name;
    private readonly ITestOutputHelper _outputHelper;

    public XUnitLogger(string name, ITestOutputHelper outputHelper)
    {
        _name = name;
        _outputHelper = EnsureArg.IsNotNull(outputHelper, nameof(outputHelper));
    }

    // TODO: Support scopes
    public IDisposable BeginScope<TState>(TState state)
        => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        EnsureArg.IsNotNull(formatter, nameof(formatter));

        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        message = $"{ logLevel }: {_name} [{eventId.Id}] {message}";
        if (exception != null)
        {
            message += Environment.NewLine + Environment.NewLine + exception;
        }

        _outputHelper.WriteLine(message);
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        { }

        public void Dispose()
        { }
    }
}
