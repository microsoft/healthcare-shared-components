﻿// -------------------------------------------------------------------------------------------------
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
        /// </summary>
        public int ConnectRetryCount { get; set; } = 4;

        /// <summary>
        /// Set SqlConnectionStringBuilder.ConnectTimeout to retry connection open transient issues in seconds
        /// </summary>
        public int ConnectTimeoutInSeconds { get; internal set; } = 30;
    }
}
