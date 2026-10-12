// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Globalization;
using System.Threading;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Health.SqlServer.UnitTests;

[TestClass]
public class SqlMetadataUtilitiesTests
{
    [TestMethod]
    public void GivenASqlMetadataInstanceWithDefaultScaleAndPrecision_WhenGettingMinAndMaxValues_ReturnsCorrectValues()
    {
        var sqlMetaData = new SqlMetaData("foo", SqlDbType.Decimal);
        Assert.AreEqual(-999999999999999999M, SqlMetadataUtilities.GetMinValueForDecimalColumn(sqlMetaData));
        Assert.AreEqual(999999999999999999M, SqlMetadataUtilities.GetMaxValueForDecimalColumn(sqlMetaData));
    }

    [TestMethod]
    public void GivenASqlMetadataInstanceWithSpecifiedScaleAndPrecision_WhenGettingMinAndMaxValues_ReturnsCorrectValues()
    {
        var sqlMetaData = new SqlMetaData("foo", SqlDbType.Decimal, precision: 10, scale: 3);
        Assert.AreEqual(-9999999.999M, SqlMetadataUtilities.GetMinValueForDecimalColumn(sqlMetaData));
        Assert.AreEqual(9999999.999M, SqlMetadataUtilities.GetMaxValueForDecimalColumn(sqlMetaData));
    }

    [TestMethod]
    public void GivenASqlMetadataInstanceWithSpecifiedScaleAndPrecisionInCommaCulture_WhenGettingMinAndMaxValues_ReturnsCorrectValues()
    {
        var sqlMetaData = new SqlMetaData("foo", SqlDbType.Decimal, precision: 10, scale: 3);
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = culture;
        Assert.AreEqual(-9999999.999M, SqlMetadataUtilities.GetMinValueForDecimalColumn(sqlMetaData));
        Assert.AreEqual(9999999.999M, SqlMetadataUtilities.GetMaxValueForDecimalColumn(sqlMetaData));
        Thread.CurrentThread.CurrentCulture = originalCulture;
    }
}
