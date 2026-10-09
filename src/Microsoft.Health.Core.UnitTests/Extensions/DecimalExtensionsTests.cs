// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.Core.Extensions.UnitTests;

[TestClass]
public class DecimalExtensionsTests
{
    [TestMethod]
    [DataRow("100", ".5")]
    [DataRow("100.0", ".05")]
    [DataRow("100.00", ".005")]
    [DataRow("100.000", ".0005")]
    [DataRow("100.010", ".0005")]
    [DataRow("100.0000", ".00005")]
    [DataRow("100.00000", ".000005")]
    [DataRow("100.12345", ".000005")]
    [DataRow(".1234567890123456789012345678", "0")]
    public void GivenADecimal_WhenGetPrecisionModifierIsCalled_ThenCorrectDecimalIsReturned(string input, string expected)
    {
        var inputDecimal = decimal.Parse(input, CultureInfo.InvariantCulture);
        var expectedDecimal = decimal.Parse(expected, CultureInfo.InvariantCulture);
        Assert.AreEqual(expectedDecimal, inputDecimal.GetPrescisionModifier());
    }
}
