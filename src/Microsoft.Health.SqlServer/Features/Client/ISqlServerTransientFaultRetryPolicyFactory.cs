// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Polly;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality to create retry policy for handling transient errors.
    /// </summary>
    public interface ISqlServerTransientFaultRetryPolicyFactory
    {
        /// <summary>
        /// Creates a retry policy.
        /// </summary>
        /// <returns>A <see cref="IAsyncPolicy"/> object.</returns>
        IAsyncPolicy Create();
    }
}
