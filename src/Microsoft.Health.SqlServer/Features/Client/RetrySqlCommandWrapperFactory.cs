// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using EnsureThat;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Extensions;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality to create an instance of <see cref="RetrySqlCommandWrapper"/>.
    /// </summary>
    internal class RetrySqlCommandWrapperFactory : SqlCommandWrapperFactory
    {
        private readonly RetryPolicy _retryPolicy;
        private readonly IAsyncPolicy _asyncRetryPolicy;

        public RetrySqlCommandWrapperFactory(
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

            Action<Exception, TimeSpan, int, Context> onRetryLogger = pollyRetryLoggerFactory.Create<RetrySqlCommandWrapper>();

            _retryPolicy = policyBuilder.WaitAndRetry(
                sleepDurations,
                onRetry: onRetryLogger);

            _asyncRetryPolicy = policyBuilder.WaitAndRetryAsync(
                sleepDurations,
                onRetry: onRetryLogger);
        }

        /// <inheritdoc/>
        public override SqlCommandWrapper Create(SqlCommand sqlCommand)
        {
            return new RetrySqlCommandWrapper(sqlCommand, _retryPolicy, _asyncRetryPolicy);
        }
    }
}
