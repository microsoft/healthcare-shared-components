// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client
{
    public class SqlConnectionWrapperFactory
    {
        private readonly SqlTransactionHandler _sqlTransactionHandler;
        private readonly SqlCommandWrapperFactory _sqlCommandWrapperFactory;
        private readonly ISqlConnectionFactory _sqlConnectionFactory;

        public SqlConnectionWrapperFactory(
            SqlTransactionHandler sqlTransactionHandler,
            SqlCommandWrapperFactory sqlCommandWrapperFactory,
            ISqlConnectionFactory sqlConnectionFactory)
        {
            EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
            EnsureArg.IsNotNull(sqlCommandWrapperFactory, nameof(sqlCommandWrapperFactory));
            EnsureArg.IsNotNull(sqlConnectionFactory, nameof(sqlConnectionFactory));

            _sqlTransactionHandler = sqlTransactionHandler;
            _sqlCommandWrapperFactory = sqlCommandWrapperFactory;
            _sqlConnectionFactory = sqlConnectionFactory;
        }

        public SqlConnectionWrapper ObtainSqlConnectionWrapper(bool enlistInTransaction = false)
        {
            return new SqlConnectionWrapper(_sqlTransactionHandler, _sqlCommandWrapperFactory, _sqlConnectionFactory, enlistInTransaction);
        }
    }
}