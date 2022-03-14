// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client;

public class SqlConnectionWrapperFactory
{
    private readonly SqlTransactionHandler _sqlTransactionHandler;
    private readonly ISqlConnectionBuilder _sqlConnectionBuilder;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;

    public SqlConnectionWrapperFactory(
        SqlTransactionHandler sqlTransactionHandler,
        ISqlConnectionBuilder sqlConnectionBuilder,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider)
    {
        EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
        EnsureArg.IsNotNull(sqlConnectionBuilder, nameof(sqlConnectionBuilder));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));

        _sqlTransactionHandler = sqlTransactionHandler;
        _sqlConnectionBuilder = sqlConnectionBuilder;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
    }

    public async Task<SqlConnectionWrapper> ObtainSqlConnectionWrapperAsync(CancellationToken cancellationToken, bool enlistInTransaction = false)
    {
        SqlConnectionWrapper sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlConnectionBuilder, _sqlRetryLogicBaseProvider, enlistInTransaction);
        await sqlConnectionWrapper.InitializeAsync(cancellationToken);

        return sqlConnectionWrapper;
    }
}
