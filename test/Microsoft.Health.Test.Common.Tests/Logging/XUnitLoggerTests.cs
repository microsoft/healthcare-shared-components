// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Health.Test.Utilities.UnitTests.Logging;

public class XUnitLoggerTests : IClassFixture<LoggingFixture>
{
    private readonly ILogger<LoggingFixture> _logger;

    public XUnitLoggerTests(ITestOutputHelper outputHelper)
    {
        _logger = new ServiceCollection()
            .AddLogging(x => x.AddXUnit(outputHelper))
            .BuildServiceProvider()
            .GetRequiredService<ILogger<LoggingFixture>>();
    }

    [Fact]
    public void GivenXUnitTest_WhenLogging_ThenWriteWithoutIssue()
    {
        _logger.LogInformation("Running test");
    }
}

public class LoggingFixture
{
    public LoggingFixture(IMessageSink sink)
    {
        ILogger<LoggingFixture> logger = new ServiceCollection()
            .AddLogging(x => x.AddXUnit(sink))
            .BuildServiceProvider()
            .GetRequiredService<ILogger<LoggingFixture>>();

        logger.LogInformation("Starting class fixture");
    }
}
