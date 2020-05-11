// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using EnsureThat;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality to create an instance of <see cref="RetrySqlCommandWrapper"/>.
    /// </summary>
    internal class RetrySqlCommandWrapperFactory : SqlCommandWrapperFactory
    {
        private readonly IAsyncPolicy _retryPolicy;

        public RetrySqlCommandWrapperFactory(ISqlServerTransientFaultRetryPolicyFactory sqlTransientFaultRetryPolicyFactory)
        {
            EnsureArg.IsNotNull(sqlTransientFaultRetryPolicyFactory, nameof(sqlTransientFaultRetryPolicyFactory));

            _retryPolicy = sqlTransientFaultRetryPolicyFactory.Create();
        }

        /// <inheritdoc/>
        public override SqlCommandWrapper Create(SqlCommand sqlCommand)
        {
            return new RetrySqlCommandWrapper(
                base.Create(sqlCommand),
                _retryPolicy);
        }
    }
}
