// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Polly;
using Polly.Retry;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// A wrapper around <see cref="SqlCommand"/> to provide automatic retries for transient errors.
    /// </summary>
    internal class RetrySqlCommandWrapper : SqlCommandWrapper
    {
        private readonly RetryPolicy _retryPolicy;
        private readonly IAsyncPolicy _asyncRetryPolicy;

        public RetrySqlCommandWrapper(SqlCommand sqlCommand, RetryPolicy retryPolicy, IAsyncPolicy asyncRetryPolicy)
            : base(sqlCommand)
        {
            EnsureArg.IsNotNull(retryPolicy, nameof(retryPolicy));
            EnsureArg.IsNotNull(asyncRetryPolicy, nameof(asyncRetryPolicy));

            _retryPolicy = retryPolicy;
            _asyncRetryPolicy = asyncRetryPolicy;
        }

        /// <inheritdoc/>
        public override int ExecuteNonQuery()
            => _retryPolicy.Execute(() => base.ExecuteNonQuery());

        /// <inheritdoc/>
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            => _asyncRetryPolicy.ExecuteAsync(() => base.ExecuteNonQueryAsync(cancellationToken));

        /// <inheritdoc/>
        public override object ExecuteScalar()
            => _retryPolicy.Execute(() => base.ExecuteScalar());

        /// <summary>
        /// <see cref="SqlCommand.ExecuteScalarAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
            => _retryPolicy.Execute(() => base.ExecuteScalarAsync(cancellationToken));

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReader"/>.
        /// </summary>
        /// <returns>A <see cref="SqlDataReader"/> object.</returns>
        public override SqlDataReader ExecuteReader()
            => _retryPolicy.Execute(() => base.ExecuteReader());

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReader(CommandBehavior)"/>.
        /// </summary>
        /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
        /// <returns>A <see cref="SqlDataReader"/> object.</returns>
        public override SqlDataReader ExecuteReader(CommandBehavior behavior)
            => _retryPolicy.Execute(() => base.ExecuteReader(behavior));

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override Task<SqlDataReader> ExecuteReaderAsync()
            => _retryPolicy.Execute(() => base.ExecuteReaderAsync());

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
            => _retryPolicy.Execute(() => base.ExecuteReaderAsync(cancellationToken));

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync(CommandBehavior)"/>.
        /// </summary>
        /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior)
            => _retryPolicy.Execute(() => base.ExecuteReaderAsync(behavior));

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/>.
        /// </summary>
        /// <param name="behavior">One of the <see cref="CommandBehavior"/>values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
            => _retryPolicy.Execute(() => base.ExecuteReaderAsync(behavior, cancellationToken));
    }
}
