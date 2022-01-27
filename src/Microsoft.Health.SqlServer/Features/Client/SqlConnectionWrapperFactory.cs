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
        private readonly ISqlConnectionBuilder _sqlConnectionBuilder;

        public SqlConnectionWrapperFactory(
            SqlTransactionHandler sqlTransactionHandler,
            SqlCommandWrapperFactory sqlCommandWrapperFactory,
            ISqlConnectionBuilder sqlConnectionBuilder)
        {
            EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
            EnsureArg.IsNotNull(sqlCommandWrapperFactory, nameof(sqlCommandWrapperFactory));
            EnsureArg.IsNotNull(sqlConnectionBuilder, nameof(sqlConnectionBuilder));

            _sqlTransactionHandler = sqlTransactionHandler;
            _sqlCommandWrapperFactory = sqlCommandWrapperFactory;
            _sqlConnectionBuilder = sqlConnectionBuilder;
        }

        public async Task<SqlConnectionWrapper> ObtainSqlConnectionWrapperAsync(CancellationToken cancellationToken, bool enlistInTransaction = false)
        {
            SqlConnectionWrapper sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlCommandWrapperFactory, _sqlConnectionBuilder, enlistInTransaction);
            await sqlConnectionWrapper.InitializeAsync(cancellationToken);

            return sqlConnectionWrapper;
        }
    }
}
