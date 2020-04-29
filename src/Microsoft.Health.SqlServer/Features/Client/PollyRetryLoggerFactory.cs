// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality for creating a logger for the <see cref="Polly"/> retry policy.
    /// </summary>
    internal class PollyRetryLoggerFactory : IPollyRetryLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public PollyRetryLoggerFactory(ILoggerFactory loggerFactory)
        {
            EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));

            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public Action<Exception, TimeSpan, int, Context> Create<TCategoryName>()
        {
            ILogger logger = _loggerFactory.CreateLogger<TCategoryName>();

            return (exception, sleepDuration, retryCount, context) =>
            {
                logger.LogWarning(exception, "The operation failed. Will retry in '{SleepDuration}'. Retried {RetryCount} of time(s) so far.");
            };
        }
    }
}
