// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Globalization;
using System.Threading;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Health.SqlServer.Features.Schema.Model;
using Xunit;

namespace Microsoft.Health.SqlServer.UnitTests
{
    public class SqlMetadataUtilitiesTests
    {
        [Fact]
        public void GivenASqlMetadataInstanceWithDefaultScaleAndPrecision_WhenGettingMinAndMaxValues_ReturnsCorrectValues()
        {
            var sqlMetaData = new SqlMetaData("foo", SqlDbType.Decimal);
            Assert.Equal(-999999999999999999M, SqlMetadataUtilities.GetMinValueForDecimalColumn(sqlMetaData));
            Assert.Equal(999999999999999999M, SqlMetadataUtilities.GetMaxValueForDecimalColumn(sqlMetaData));
        }

        [Fact]
        public void GivenASqlMetadataInstanceWithSpecifiedScaleAndPrecision_WhenGettingMinAndMaxValues_ReturnsCorrectValues()
        {
            var sqlMetaData = new SqlMetaData("foo", SqlDbType.Decimal, precision: 10, scale: 3);
            Assert.Equal(-9999999.999M, SqlMetadataUtilities.GetMinValueForDecimalColumn(sqlMetaData));
            Assert.Equal(9999999.999M, SqlMetadataUtilities.GetMaxValueForDecimalColumn(sqlMetaData));
        }

        [Fact]
        public void GivenASqlMetadataInstanceWithSpecifiedScaleAndPrecisionInCommaCulture_WhenGettingMinAndMaxValues_ReturnsCorrectValues()
        {
            var sqlMetaData = new SqlMetaData("foo", SqlDbType.Decimal, precision: 10, scale: 3);
            CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NumberGroupSeparator = ".";
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = culture;
            Assert.Equal(-9999999.999M, SqlMetadataUtilities.GetMinValueForDecimalColumn(sqlMetaData));
            Assert.Equal(9999999.999M, SqlMetadataUtilities.GetMaxValueForDecimalColumn(sqlMetaData));
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }
}