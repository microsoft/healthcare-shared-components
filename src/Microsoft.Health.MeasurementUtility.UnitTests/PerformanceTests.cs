// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.MeasurementUtility.UnitTests;

[TestClass]
public class PerformanceTests
{
    [TestMethod]
    public void GivenTheITimed_WhenBeingDisposed_ThenHandlerShouldBeInvoked()
    {
        bool hasHandlerInvoked = false;
        using (ITimed timedHandler = Performance.TrackDuration(duration =>
        {
            hasHandlerInvoked = true;
            Assert.IsGreaterThan(1000, duration);
        }))
        {
            Thread.Sleep(1000);
        }

        Assert.IsTrue(hasHandlerInvoked);
    }
}
