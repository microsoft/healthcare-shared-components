// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Globalization;
using EnsureThat;
using Microsoft.Data.SqlClient.Server;

namespace Microsoft.Health.SqlServer.Features.Schema.Model
{
    internal static class SqlMetadataUtilities
    {
        internal static decimal GetMinValueForDecimalColumn(SqlMetaData columnMetaData)
        {
            return GetMinOrMaxSqlDecimalValueForColumn(columnMetaData, min: true);
        }

        internal static decimal GetMaxValueForDecimalColumn(SqlMetaData columnMetaData)
        {
            return GetMinOrMaxSqlDecimalValueForColumn(columnMetaData, min: false);
        }

        private static decimal GetMinOrMaxSqlDecimalValueForColumn(SqlMetaData columnMetadata, bool min)
        {
            EnsureArg.IsNotNull(columnMetadata, nameof(columnMetadata));
            EnsureArg.Is((int)SqlDbType.Decimal, (int)columnMetadata.SqlDbType, nameof(columnMetadata));
            var separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
            var val = decimal.Parse(
                $"{new string('9', columnMetadata.Precision - columnMetadata.Scale)}{separator}{new string('9', columnMetadata.Scale)}",
                CultureInfo.InvariantCulture);
            return min ? -val : val;
        }

        internal static double GetMinValueForFloatColumn(SqlMetaData columnMetaData)
        {
            return GetMinOrMaxSqlFloatValueForColumn(columnMetaData, min: true);
        }

        internal static double GetMaxValueForFloatColumn(SqlMetaData columnMetaData)
        {
            return GetMinOrMaxSqlFloatValueForColumn(columnMetaData, min: false);
        }

        private static double GetMinOrMaxSqlFloatValueForColumn(SqlMetaData columnMetadata, bool min)
        {
            EnsureArg.IsNotNull(columnMetadata, nameof(columnMetadata));
            EnsureArg.Is((int)SqlDbType.Float, (int)columnMetadata.SqlDbType, nameof(columnMetadata));

            if (columnMetadata.Precision <= 24 && columnMetadata.Precision >= 1)
            {
                return min ? Math.Round(double.MinValue, 24) : Math.Round(double.MaxValue, 24);
            }
            else if (columnMetadata.Precision >= 25 && columnMetadata.Precision <= 53)
            {
                return min ? Math.Round(double.MinValue, 53) : Math.Round(double.MaxValue, 53);
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Float columns must have precision {0} between 1 and 53", columnMetadata.Precision));
            }
        }
    }
}
