// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Xunit;

namespace Microsoft.Health.MeasurementUtility.UnitTests;

public class PerformanceTests
{
    [Fact]
    public void GivenTheITimed_WhenBeingDisposed_ThenHandlerShouldBeInvoked()
    {
        bool hasHandlerInvoked = false;
        using (ITimed timedHandler = Performance.TrackDuration(duration =>
        {
            hasHandlerInvoked = true;
            Assert.True(duration > 1000);
        }))
        {
            Thread.Sleep(1000);
        }

        Assert.True(hasHandlerInvoked);
    }
}
