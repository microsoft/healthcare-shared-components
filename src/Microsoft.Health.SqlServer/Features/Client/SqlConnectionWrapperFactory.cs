// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client
{
    public class SqlConnectionWrapperFactory
    {
        private readonly SqlTransactionHandler _sqlTransactionHandler;
        private readonly SqlCommandWrapperFactory _sqlCommandWrapperFactory;
        private readonly ISqlConnection _sqlConnection;

        public SqlConnectionWrapperFactory(
            SqlTransactionHandler sqlTransactionHandler,
            SqlCommandWrapperFactory sqlCommandWrapperFactory,
            ISqlConnection sqlConnection)
        {
            EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
            EnsureArg.IsNotNull(sqlCommandWrapperFactory, nameof(sqlCommandWrapperFactory));
            EnsureArg.IsNotNull(sqlConnection, nameof(sqlConnection));

            _sqlTransactionHandler = sqlTransactionHandler;
            _sqlCommandWrapperFactory = sqlCommandWrapperFactory;
            _sqlConnection = sqlConnection;
        }

        public async Task<SqlConnectionWrapper> ObtainSqlConnectionWrapperAsync(CancellationToken cancellationToken, bool enlistInTransaction = false)
        {
            SqlConnectionWrapper sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlCommandWrapperFactory, _sqlConnection, enlistInTransaction);
            await sqlConnectionWrapper.InitializeAsync(cancellationToken);

            return sqlConnectionWrapper;
        }
    }
}
