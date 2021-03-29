// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// A wrapper around <see cref="SqlCommand"/> to provide automatic retries for transient errors.
    /// </summary>
    public class RetrySqlCommandWrapper : SqlCommandWrapper
    {
        private readonly SqlCommandWrapper _sqlCommandWrapper;
        private readonly IAsyncPolicy _retryPolicy;

        public RetrySqlCommandWrapper(SqlCommandWrapper sqlCommandWrapper, IAsyncPolicy retryPolicy)
            : base(sqlCommandWrapper)
        {
            EnsureArg.IsNotNull(sqlCommandWrapper, nameof(sqlCommandWrapper));
            EnsureArg.IsNotNull(retryPolicy, nameof(retryPolicy));

            _sqlCommandWrapper = sqlCommandWrapper;
            _retryPolicy = retryPolicy;
        }

        /// <inheritdoc/>
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken));

        /// <inheritdoc/>
        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
            => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteScalarAsync(cancellationToken));

        /// <inheritdoc/>
        public override Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
            => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteReaderAsync(cancellationToken));

        /// <inheritdoc/>
        public override Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
            => _retryPolicy.ExecuteAsync(() => _sqlCommandWrapper.ExecuteReaderAsync(behavior, cancellationToken));
    }
}
