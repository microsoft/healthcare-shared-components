// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Test.Utilities.Logging;
using Xunit.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering new providers on an <see cref="ILoggingBuilder"/> instance.
/// </summary>
public static class LoggingRegistrationExtensions
{
    /// <summary>
    /// Adds an <see cref="XUnitLoggerProvider"/> to the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="sink">A diagnostic message sink.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="sink"/> is <see langword="null"/>.
    /// </exception>
    public static ILoggingBuilder AddXUnit(this ILoggingBuilder builder, IMessageSink sink)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(sink, nameof(sink));

        builder.Services.AddSingleton<ILoggerProvider>(_ => new XUnitLoggerProvider(sink));
        return builder;
    }

    /// <summary>
    /// Adds an <see cref="XUnitLoggerProvider"/> to the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
    /// <param name="outputHelper">A console-like outputter.</param>
    /// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="outputHelper"/> is <see langword="null"/>.
    /// </exception>
    public static ILoggingBuilder AddXUnit(this ILoggingBuilder builder, ITestOutputHelper outputHelper)
    {
        EnsureArg.IsNotNull(builder, nameof(builder));
        EnsureArg.IsNotNull(outputHelper, nameof(outputHelper));

        builder.Services.AddSingleton<ILoggerProvider>(_ => new XUnitLoggerProvider(outputHelper));
        return builder;
    }
}
