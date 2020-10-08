﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.Abstractions.Features.Transactions;

namespace Microsoft.Health.SqlServer.Features.Storage
{
    public class SqlTransactionScope : ITransactionScope
    {
        private bool _isDisposed;
        private readonly SqlTransactionHandler _sqlTransactionHandler;

        public SqlTransactionScope(SqlTransactionHandler sqlTransactionHandler)
        {
            EnsureArg.IsNotNull(sqlTransactionHandler, nameof(SqlTransactionHandler));

            _sqlTransactionHandler = sqlTransactionHandler;
        }

        public SqlConnection SqlConnection { get; set; }

        public SqlTransaction SqlTransaction { get; set; }

        public void Complete()
        {
            SqlTransaction?.Commit();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                SqlConnection?.Dispose();
                SqlTransaction?.Dispose();

                SqlConnection = null;
                SqlTransaction = null;

                _isDisposed = true;

                _sqlTransactionHandler.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}