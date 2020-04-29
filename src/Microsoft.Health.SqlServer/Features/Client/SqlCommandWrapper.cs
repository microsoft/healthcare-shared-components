// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// A wrapper around <see cref="SqlCommand"/> to allow extensibility.
    /// </summary>
    public class SqlCommandWrapper : IDbCommand
    {
        private readonly SqlCommand _sqlCommand;

        public SqlCommandWrapper(SqlCommand sqlCommand)
        {
            EnsureArg.IsNotNull(sqlCommand, nameof(sqlCommand));

            _sqlCommand = sqlCommand;
        }

        /// <inheritdoc/>
        public CommandType CommandType
        {
            get => _sqlCommand.CommandType;
            set => _sqlCommand.CommandType = value;
        }

        /// <inheritdoc/>
        public int CommandTimeout
        {
            get => _sqlCommand.CommandTimeout;
            set => _sqlCommand.CommandTimeout = value;
        }

        /// <inheritdoc/>
        public string CommandText
        {
            get => _sqlCommand.CommandText;
            set => _sqlCommand.CommandText = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="SqlConnection"/> used by this instance of the <see cref="SqlCommandWrapper"/>.
        /// </summary>
        public SqlConnection Connection
        {
            get => _sqlCommand.Connection;
            set => _sqlCommand.Connection = value;
        }

        /// <summary>
        /// Gets or sets a value that specifies the <see cref="SqlNotificationRequest"/> object bound to this command.
        /// </summary>
        public SqlNotificationRequest Notification
        {
            get => _sqlCommand.Notification;
            set => _sqlCommand.Notification = value;
        }

        /// <summary>
        /// Gets the <see cref="SqlParameterCollection"/>.
        /// </summary>
        public SqlParameterCollection Parameters => _sqlCommand.Parameters;

        /// <summary>
        /// Gets or sets the <see cref="SqlTransaction"/> within which the <see cref="SqlCommand"/> executes.
        /// </summary>
        public SqlTransaction Transaction
        {
            get => _sqlCommand.Transaction;
            set => _sqlCommand.Transaction = value;
        }

        /// <inheritdoc/>
        IDbConnection IDbCommand.Connection
        {
            get => _sqlCommand.Connection;
            set
            {
                EnsureArg.IsOfType(value, typeof(SqlConnection), nameof(value));

                _sqlCommand.Connection = (SqlConnection)value;
            }
        }

        /// <inheritdoc/>
        IDataParameterCollection IDbCommand.Parameters => _sqlCommand.Parameters;

        /// <inheritdoc/>
        IDbTransaction IDbCommand.Transaction
        {
            get => _sqlCommand.Transaction;
            set
            {
                EnsureArg.IsOfType(value, typeof(SqlTransaction), nameof(value));

                _sqlCommand.Transaction = (SqlTransaction)value;
            }
        }

        /// <inheritdoc/>
        public UpdateRowSource UpdatedRowSource
        {
            get => _sqlCommand.UpdatedRowSource;
            set => _sqlCommand.UpdatedRowSource = value;
        }

        /// <inheritdoc/>
        public virtual void Cancel()
            => _sqlCommand.Cancel();

        /// <summary>
        /// Creates a new instance of a <see cref="SqlParameter"/> object.
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

        /// <inheritdoc/>
        public virtual int ExecuteNonQuery()
            => _sqlCommand.ExecuteNonQuery();

        /// <summary>
        /// <see cref="SqlCommand.ExecuteNonQueryAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            => _sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        /// <inheritdoc/>
        public virtual object ExecuteScalar()
            => _sqlCommand.ExecuteScalar();

        /// <summary>
        /// <see cref="SqlCommand.ExecuteScalarAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
            => _sqlCommand.ExecuteScalarAsync(cancellationToken);

        /// <inheritdoc/>
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
        /// <see cref="SqlCommand.ExecuteReaderAsync"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<SqlDataReader> ExecuteReaderAsync()
            => _sqlCommand.ExecuteReaderAsync();

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<SqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
            => _sqlCommand.ExecuteReaderAsync(cancellationToken);

        /// <summary>
        /// <see cref="SqlCommand.ExecuteReaderAsync(CommandBehavior)"/>.
        /// </summary>
        /// <param name="behavior">One of the <see cref="CommandBehavior"/> values.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task<SqlDataReader> ExecuteReaderAsync(CommandBehavior behavior)
            => _sqlCommand.ExecuteReaderAsync(behavior);

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

        /// <inheritdoc/>
        IDbDataParameter IDbCommand.CreateParameter()
            => CreateParameter();

        /// <inheritdoc/>
        IDataReader IDbCommand.ExecuteReader()
            => ExecuteReader();

        /// <inheritdoc/>
        IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior)
            => ExecuteReader(behavior);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sqlCommand.Dispose();
            }
        }
    }
}
