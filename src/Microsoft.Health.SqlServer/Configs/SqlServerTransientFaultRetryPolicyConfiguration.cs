// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Health.SqlServer.Configs
{
    /// <summary>
    /// Configuration for transient fault retry policy.
    /// </summary>
    public class SqlServerTransientFaultRetryPolicyConfiguration
    {
        /// <summary>
        /// The duration value for the wait before the first retry.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// The maximum number of retries to use, in addition to the original call for SqlCommand Execute*.
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// The exponent to multiply each subsequent duration by.
        /// </summary>
        public int Factor { get; set; } = 2;

        /// <summary>
        /// Whether the first retry will be immediate or not.
        /// </summary>
        public bool FastFirst { get; set; } = true;

        /// <summary>
        /// Set SqlConnectionStringBuilder.ConnectConnectRetryCount to retry connection open transient issues
        /// Range is 0 through 255
        /// </summary>
        public int ConnectRetryCount { get; set; } = 3;

        /// <summary>
        /// Set SqlConnectionStringBuilder.ConnectTimeout to retry connection open transient issues in seconds
        /// Range is 1 through 60
        /// Make sure ConnectionTimeout = ConnectRetryCount * ConnectionRetryInterval
        /// https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-connectivity-issues#net-sqlconnection-parameters-for-connection-retry
        /// </summary>
        public int ConnectTimeoutInSeconds { get; set; } = 30;

        /// <summary>
        /// Set SqlConnectionStringBuilder.ConnectRetryInterval to retry connection open transient issues in seconds
        /// Range is 0 through 2147483647.
        /// </summary>
        public int ConnectRetryIntervalInSeconds { get; set; } = 10;
    }
}
