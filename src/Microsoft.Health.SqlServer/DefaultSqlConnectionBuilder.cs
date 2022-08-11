// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

/// <summary>
/// Default Sql Connection Factory is responsible to handle Sql connections that can be made purely based on connection string.
/// Connection string containing user name and password, or integrated auth are perfect examples for this.
/// </summary>
public class DefaultSqlConnectionBuilder : ISqlConnectionBuilder
{
    private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;

    public DefaultSqlConnectionBuilder(
        ISqlConnectionStringProvider sqlConnectionStringProvider,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider)
    {
        EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));

        _sqlConnectionStringProvider = sqlConnectionStringProvider;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
    }

    /// <inheritdoc />
    public Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
    {
        return SqlConnectionHelper.GetBaseSqlConnectionAsync(
            _sqlConnectionStringProvider,
            _sqlRetryLogicBaseProvider,
            initialCatalog,
            cancellationToken);
    }
}
