// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Health.Test.Utilities.UnitTests.Logging;

public class XUnitLoggerTests
{
    [Fact]
    public void GivenXUnitOutputHelper_WhenLogging_ThenWriteLine()
    {
        ITestOutputHelper outputHelper = Substitute.For<ITestOutputHelper>();

        ILogger<XUnitLoggerTests> logger = new ServiceCollection()
            .AddLogging(x => x.AddXUnit(outputHelper))
            .BuildServiceProvider()
            .GetRequiredService<ILogger<XUnitLoggerTests>>();

        logger.LogInformation("Hello World");

        outputHelper
            .Received(1)
            .WriteLine(Arg.Is<string>(x => x.Contains("Hello World", StringComparison.Ordinal)));
    }

    [Fact]
    public void GivenXUnitMessageSink_WhenLogging_ThenWriteLine()
    {
        IMessageSink sink = Substitute.For<IMessageSink>();

        ILogger<XUnitLoggerTests> logger = new ServiceCollection()
            .AddLogging(x => x.AddXUnit(sink))
            .BuildServiceProvider()
            .GetRequiredService<ILogger<XUnitLoggerTests>>();

        logger.LogInformation("Hello World");

        sink
            .Received(1)
            .OnMessage(Arg.Is<DiagnosticMessage>(x => x.Message.Contains("Hello World", StringComparison.Ordinal)));
    }
}
