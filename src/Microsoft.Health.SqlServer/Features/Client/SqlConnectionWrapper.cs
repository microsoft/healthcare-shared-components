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
        private readonly ISqlConnectionFactory _sqlConnection;

        public SqlConnectionWrapper(
            SqlTransactionHandler sqlTransactionHandler,
            SqlCommandWrapperFactory sqlCommandWrapperFactory,
            ISqlConnectionFactory sqlConnection,
            bool enlistInTransactionIfPresent)
        {
            EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
            EnsureArg.IsNotNull(sqlCommandWrapperFactory, nameof(sqlCommandWrapperFactory));
            EnsureArg.IsNotNull(sqlConnection, nameof(sqlConnection));

            _sqlTransactionHandler = sqlTransactionHandler;
            _enlistInTransactionIfPresent = enlistInTransactionIfPresent;
            _sqlCommandWrapperFactory = sqlCommandWrapperFactory;
            _sqlConnection = sqlConnection;

            if (_enlistInTransactionIfPresent && sqlTransactionHandler.SqlTransactionScope?.SqlConnection != null)
            {
                SqlConnection = sqlTransactionHandler.SqlTransactionScope.SqlConnection;
            }
            else
            {
                SqlConnection = _sqlConnection.GetSqlConnection();
            }

            if (_enlistInTransactionIfPresent && sqlTransactionHandler.SqlTransactionScope != null && sqlTransactionHandler.SqlTransactionScope.SqlConnection == null)
            {
                sqlTransactionHandler.SqlTransactionScope.SqlConnection = SqlConnection;
            }

            if (SqlConnection.State != ConnectionState.Open)
            {
                SqlConnection.Open();
            }

            if (enlistInTransactionIfPresent && sqlTransactionHandler.SqlTransactionScope != null)
            {
                SqlTransaction = sqlTransactionHandler.SqlTransactionScope.SqlTransaction ?? SqlConnection.BeginTransaction();

                if (sqlTransactionHandler.SqlTransactionScope.SqlTransaction == null)
                {
                    sqlTransactionHandler.SqlTransactionScope.SqlTransaction = SqlTransaction;
                }
            }
        }

        public SqlConnection SqlConnection { get; }

        public SqlTransaction SqlTransaction { get; }

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
