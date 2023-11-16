// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlTypes;
using System.Globalization;
using Microsoft.Data.SqlClient.Server;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

internal static class ColumnUtilities
{
    internal static long GetLengthForFloatColumn(byte precision)
    {
        if (precision >= 1 && precision <= 24)
        {
            return 4;
        }
        else if (precision >= 25 && precision <= 53)
        {
            return 8;
        }
        else
        {
            throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, "Precision {0} must be between 1 & 53", precision));
        }
    }
    internal static void ValidateLength(SqlMetaData sqlMetaData, decimal value)
    {
        if (((SqlDecimal)value).Precision > sqlMetaData.Precision || ((SqlDecimal)value).Scale > sqlMetaData.Scale)
        {
            throw new SqlTruncateException(string.Format(CultureInfo.CurrentCulture, FormatResources.DecimalValueOutOfRange, value, sqlMetaData.Precision, sqlMetaData.Scale));
        }
    }
}
