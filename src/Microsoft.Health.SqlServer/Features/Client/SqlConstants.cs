// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Client;

public static class SqlConstants
{
    /// <summary>
    /// The max pool size name used in connection string.
    /// </summary>
    public const string MaxPoolSizeName = "Max Pool Size";

    /// <summary>
    /// The maximum max pool size limit.
    /// </summary>
    public const int MaxPoolSizeLimit = 30000;
}
