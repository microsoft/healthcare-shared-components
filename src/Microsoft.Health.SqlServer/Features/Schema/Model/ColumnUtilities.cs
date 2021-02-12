// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Features.Schema.Model
{
    internal static class ColumnUtilities
    {
        internal static long GetLengthForFloatColumn(byte precision)
        {
            if (precision >= 1 && precision <= 23)
            {
                return 4;
            }
            else if (precision >= 24 && precision <= 53)
            {
                return 8;
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Precision {0} must be between 1 & 53", precision));
            }
        }
    }
}
