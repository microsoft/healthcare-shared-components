﻿// -------------------------------------------------------------------------------------------------
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
    internal class RetrySqlCommandWrapper : SqlCommandWrapper
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
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                await EnsureConnectionOpenAsync(cancellationToken);
                return await _sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
            });
        }

        /// <inheritdoc/>
        public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                await EnsureConnectionOpenAsync(cancellationToken);
                return await _sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
            });
        }

        /// <inheritdoc/>
        public override Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                await EnsureConnectionOpenAsync(cancellationToken);
                return await _sqlCommandWrapper.ExecuteReaderAsync(cancellationToken);
            });
        }

        /// <inheritdoc/>
        public override Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return _retryPolicy.ExecuteAsync(async () =>
            {
                await EnsureConnectionOpenAsync(cancellationToken);
                return await _sqlCommandWrapper.ExecuteReaderAsync(behavior, cancellationToken);
            });
        }

        private Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
        {
            // null check on connection is to handle unit test that cannot mock a sealed sqlConnection type
            if (_sqlCommandWrapper.Connection != null && _sqlCommandWrapper.Connection.State != ConnectionState.Open)
            {
                return _sqlCommandWrapper.Connection.OpenAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
