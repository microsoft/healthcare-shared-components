// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality for creating a logger for the <see cref="Polly"/> retry policy.
    /// </summary>
    public interface IPollyRetryLoggerFactory
    {
        /// <summary>
        /// Creates a logger.
        /// </summary>
        /// <returns>A logger delegate.</returns>
        Action<Exception, TimeSpan, int, Context> Create();
    }
}
