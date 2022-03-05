// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Configs
{
    /// <summary>
    /// Configuration for transient fault retry policy.
    /// </summary>
    public class SqlServerTransientFaultRetryPolicyConfiguration
    {
        /// <summary>
        /// Sql Connect RetryCount to retry connection open transient issues
        /// Range is 0 through 255
        /// </summary>
        public int ConnectRetryCount { get; set; } = 5;

        /// <summary>
        /// Maximum gap time for each delay time before retry
        /// </summary>
        public int ConnectMaxTimeIntervalInSeconds { get; set; } = 20;

        /// <summary>
        /// Sql Connect Preferred gap time to delay before retry
        /// </summary>
        public int ConnectRetryIntervalInSeconds { get; set; } = 1;
    }
}
