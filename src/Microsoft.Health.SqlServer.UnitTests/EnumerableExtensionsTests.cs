// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests;

[TestClass]
public class EnumerableExtensionsTests
{
    [DataRow(null)]
    [DataRow("")]
    [TestMethod]
    public void GivenAnNullOrEmptyEnumerable_WhenCallingNullIfEmpty_ReturnsNull(string commaSeparatedInput)
    {
        string[] input = commaSeparatedInput?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.IsNull(input.NullIfEmpty());
    }

    [DataRow("1")]
    [DataRow("1,2")]
    [DataRow("1,2,3")]
    [TestMethod]
    public void GivenANonEmptyEnumerable_WhenCallingNullIfEmpty_ReturnsTheExpectedSequence(string commaSeparatedInput)
    {
        string[] inputSequence = commaSeparatedInput?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<string> wrappedSequence = inputSequence.NullIfEmpty();
        Assert.AreEqual(commaSeparatedInput, string.Join(",", wrappedSequence));
        Assert.AreEqual(commaSeparatedInput, string.Join(",", wrappedSequence));
    }
}
