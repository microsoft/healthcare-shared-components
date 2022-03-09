// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// A wrapper around <see cref="SqlCommand"/> to provide automatic retries for transient errors.
    /// </summary>
    public class RetrySqlCommandWrapper : SqlCommandWrapper
    {
        public RetrySqlCommandWrapper(SqlCommand sqlCommand, SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider)
            : base(sqlCommand)
        {
            EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));

            RetryLogicProvider = sqlRetryLogicBaseProvider;
        }

        /// <inheritdoc/>
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
           await EnsureConnectionOpenAsync(cancellationToken);
           return await base.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectionOpenAsync(cancellationToken);
            return await base.ExecuteScalarAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            await EnsureConnectionOpenAsync(cancellationToken);
            return await base.ExecuteReaderAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override async Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
           await EnsureConnectionOpenAsync(cancellationToken);
           return await base.ExecuteReaderAsync(behavior, cancellationToken);
        }

        private Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
        {
            // null check on connection is to handle unit test that cannot mock a sealed sqlConnection type
            if (Connection != null && Connection.State != ConnectionState.Open)
            {
                return Connection.OpenAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}
