﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Xunit;

namespace Microsoft.Health.Core.Extensions.UnitTests;

public class DateTimeExtensionsTests
{
    [Fact]
    public void GivenADateTime_WhenTruncated_HasNoFractionalMilliseconds()
    {
        var dateTime = new DateTime(2019, 1, 1);
        Assert.Equal(dateTime, dateTime.AddTicks(1).TruncateToMillisecond());
    }
}
