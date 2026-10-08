// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Core.Extensions.UnitTests;

[TestClass]
public class DateTimeExtensionsTests
{
    [TestMethod]
    public void GivenADateTime_WhenTruncated_HasNoFractionalMilliseconds()
    {
        var dateTime = new DateTime(2019, 1, 1);
        Assert.AreEqual(dateTime, dateTime.AddTicks(1).TruncateToMillisecond());
    }
}
