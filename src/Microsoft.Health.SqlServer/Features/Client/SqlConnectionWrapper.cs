// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client
{
    public class SqlConnectionWrapper : IDisposable
    {
        private readonly bool _enlistInTransactionIfPresent;
        private readonly SqlTransactionHandler _sqlTransactionHandler;
        private readonly SqlCommandWrapperFactory _sqlCommandWrapperFactory;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        private SqlConnection _sqlConnection;
        private SqlTransaction _sqlTransaction;

        internal SqlConnectionWrapper(
            SqlTransactionHandler sqlTransactionHandler,
            SqlCommandWrapperFactory sqlCommandWrapperFactory,
            ISqlConnectionFactory sqlConnectionFactory,
            bool enlistInTransactionIfPresent)
        {
            EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
            EnsureArg.IsNotNull(sqlCommandWrapperFactory, nameof(sqlCommandWrapperFactory));
            EnsureArg.IsNotNull(sqlConnectionFactory, nameof(sqlConnectionFactory));

            _sqlTransactionHandler = sqlTransactionHandler;
            _enlistInTransactionIfPresent = enlistInTransactionIfPresent;
            _sqlCommandWrapperFactory = sqlCommandWrapperFactory;
            _sqlConnectionFactory = sqlConnectionFactory;
        }

        public SqlConnection SqlConnection
        {
            get { return _sqlConnection; }
        }

        public SqlTransaction SqlTransaction
        {
            get
            {
                return _sqlTransaction;
            }
        }

        internal async void InitializeAsync()
        {
            if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope?.SqlConnection != null)
            {
                _sqlConnection = _sqlTransactionHandler.SqlTransactionScope.SqlConnection;
            }
            else
            {
                _sqlConnection = await _sqlConnectionFactory.GetSqlConnectionAsync();
            }

            if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope != null && _sqlTransactionHandler.SqlTransactionScope.SqlConnection == null)
            {
                _sqlTransactionHandler.SqlTransactionScope.SqlConnection = SqlConnection;
            }

            if (SqlConnection.State != ConnectionState.Open)
            {
                SqlConnection.Open();
            }

            if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope != null)
            {
                _sqlTransaction = _sqlTransactionHandler.SqlTransactionScope.SqlTransaction ?? SqlConnection.BeginTransaction();

                if (_sqlTransactionHandler.SqlTransactionScope.SqlTransaction == null)
                {
                    _sqlTransactionHandler.SqlTransactionScope.SqlTransaction = SqlTransaction;
                }
            }
        }

        public SqlCommandWrapper CreateSqlCommand()
        {
            SqlCommand sqlCommand = SqlConnection.CreateCommand();

            sqlCommand.Transaction = SqlTransaction;

            return _sqlCommandWrapperFactory.Create(sqlCommand);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_enlistInTransactionIfPresent || _sqlTransactionHandler.SqlTransactionScope == null)
                {
                    SqlConnection?.Dispose();
                    SqlTransaction?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
