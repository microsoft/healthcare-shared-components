// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Operations.UnitTests;

[TestClass]
public class OperationIdTests
{
    [TestMethod]
    public void GivenOperationIdClass_WhenGeneratingNewId_ThenReturnProperlyFormattedString()
    {
        string actual = OperationId.Generate();
        Assert.IsTrue(Guid.TryParseExact(actual, OperationId.FormatSpecifier, out Guid _));
    }

    [TestMethod]
    public void GivenString_WhenParsingOperationIdExactly_ThenReturnGuid()
    {
        Guid expected = Guid.NewGuid();
        Assert.AreEqual(expected, OperationId.ParseExact(expected.ToString(OperationId.FormatSpecifier)));
    }

    [TestMethod]
    public void GivenNull_WhenParsingOperationIdExactly_ThenThrowArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => OperationId.ParseExact(null!));

    [TestMethod]
    public void GivenInvalidString_WhenParsingOperationIdExactly_ThenThrowFormatException()
        => Assert.Throws<FormatException>(() => OperationId.ParseExact(Guid.NewGuid().ToString("X")));
}
