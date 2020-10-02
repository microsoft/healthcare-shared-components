// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.Sql;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// A wrapper around <see cref="SqlCommand"/> to allow extensibility.
    /// </summary>
    public class SqlCommandWrapper : IDisposable
    {
        private readonly SqlCommand _sqlCommand;

        public SqlCommandWrapper(SqlCommand sqlCommand)
        {
            EnsureArg.IsNotNull(sqlCommand, nameof(sqlCommand));

            _sqlCommand = sqlCommand;
        }

        protected SqlCommandWrapper(SqlCommandWrapper sqlCommandWrapper)
        {
            EnsureArg.IsNotNull(sqlCommandWrapper, nameof(sqlCommandWrapper));

            _sqlCommand = sqlCommandWrapper._sqlCommand;
        }

        /// <summary>
        /// <see cref="SqlCommand.CommandType"/>.
        /// </summary>
        public CommandType CommandType
        {
            get => _sqlCommand.CommandType;
            set => _sqlCommand.CommandType = value;
        }

        /// <summary>
        /// <see cref="SqlCommand.CommandTimeout"/>.
        /// </summary>
        public int CommandTimeout
        {
            get => _sqlCommand.CommandTimeout;
            set => _sqlCommand.CommandTimeout = value;
        }

        /// <summary>
        /// <see cref="SqlCommand.CommandText"/>.
        /// </summary>
        public string CommandText
        {
            get => _sqlCommand.CommandText;
            set => _sqlCommand.CommandText = value;
        }

        /// <summary>
        /// <see cref="SqlCommand.Connection"/>.
        /// </summary>
        public SqlConnection Connection
        {
            get => _sqlCommand.Connection;
            set => _sqlCommand.Connection = value;
        }

        /// <summary>
        /// <see cref="SqlCommand.Notification"/>.
        /// </summary>
        public SqlNotificationRequest Notification
        {
            get => _sqlCommand.Notification;
            set => _sqlCommand.Notification = value;
        }

        /// <summary>
        /// <see cref="SqlCommand.Parameters"/>.
        /// </summary>
        public SqlParameterCollection Parameters => _sqlCommand.Parameters;

        /// <summary>
        /// <see cref="SqlCommand.Transaction"/>.
        /// </summary>
        public SqlTransaction Transaction
        {
            get => _sqlCommand.Transaction;
            set => _sqlCommand.Transaction = value;
        }

        /// <summary>
        /// <see cref="SqlCommand.Cancel"/>.
        /// </summary>
        public virtual void Cancel()
            => _sqlCommand.Cancel();

        /// <summary>
        /// <see cref="SqlCommand.CreateParameter"/>.
        /// </summary>
        /// <returns>A <see cref="SqlParameter"/> object.</returns>
        public virtual SqlParameter CreateParameter()
            => _sqlCommand.CreateParameter();

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <see cref="SqlCommand.ExecuteNonQueryAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            => _sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        /// <summary>
        /// <see cref="SqlCommand.ExecuteScalarAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
            => _sqlCommand.ExecuteScalarAsync(cancellationToken);

        public virtual void Prepare()
            => _sqlCommand.Prepare();

        /// <summary>
        /// <see cref="System.Data.Common.DbCommand.PrepareAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task PrepareAsync(CancellationToken cancellationToken)
            => _sqlCommand.PrepareAsync(cancellationToken);

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReader"/>.
        /// </summary>
        /// <returns>A <see cref="SqlDataReader"/> object.</returns>
        public virtual SqlDataReader ExecuteReader()
            => _sqlCommand.ExecuteReader();

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReader(CommandBehavior)"/>.
        /// </summary>
        /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
        /// <returns>A <see cref="SqlDataReader"/> object.</returns>
        public virtual SqlDataReader ExecuteReader(CommandBehavior behavior)
            => _sqlCommand.ExecuteReader(behavior);

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
            => _sqlCommand.ExecuteReaderAsync(cancellationToken);

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync(CommandBehavior, CancellationToken)"/>.
        /// </summary>
        /// <param name="behavior">One of the <see cref="CommandBehavior"/>values.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
            => _sqlCommand.ExecuteReaderAsync(behavior, cancellationToken);

        /// <summary>
        /// <see cref="SqlCommand.ResetCommandTimeout"/>.
        /// </summary>
        public virtual void ResetCommandTimeout()
            => _sqlCommand.ResetCommandTimeout();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sqlCommand.Dispose();
            }
        }
    }
}
