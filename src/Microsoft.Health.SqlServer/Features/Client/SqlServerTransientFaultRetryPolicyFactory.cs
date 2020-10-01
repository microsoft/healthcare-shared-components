// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Extensions;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality to create retry policy for handling transient errors.
    /// </summary>
    internal class SqlServerTransientFaultRetryPolicyFactory : ISqlServerTransientFaultRetryPolicyFactory
    {
        private readonly IAsyncPolicy _retryPolicy;

        public SqlServerTransientFaultRetryPolicyFactory(
            SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
            IPollyRetryLoggerFactory pollyRetryLoggerFactory)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(pollyRetryLoggerFactory, nameof(pollyRetryLoggerFactory));

            SqlServerTransientFaultRetryPolicyConfiguration transientFaultRetryPolicyConfiguration = sqlServerDataStoreConfiguration.TransientFaultRetryPolicy;

            IEnumerable<TimeSpan> sleepDurations = Backoff.ExponentialBackoff(
                transientFaultRetryPolicyConfiguration.InitialDelay,
                transientFaultRetryPolicyConfiguration.RetryCount,
                transientFaultRetryPolicyConfiguration.Factor,
                transientFaultRetryPolicyConfiguration.FastFirst);

            PolicyBuilder policyBuilder = Policy
                .Handle<SqlException>(sqlException => sqlException.IsTransient())
                .Or<TimeoutException>();

            Action<Exception, TimeSpan, int, Context> onRetryLogger = pollyRetryLoggerFactory.Create();

            _retryPolicy = policyBuilder.WaitAndRetryAsync(
                sleepDurations,
                onRetry: onRetryLogger);
        }

        /// <inheritdoc/>
        public IAsyncPolicy Create()
            => _retryPolicy;
    }
}
