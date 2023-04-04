// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

public class ManagedIdentitySqlConnectionBuilder : ISqlConnectionBuilder
{
    private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;
    private readonly IAccessTokenHandler _accessTokenHandler;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private const string AzureResourceScope = "https://database.windows.net/.default";

    public ManagedIdentitySqlConnectionBuilder(
        ISqlConnectionStringProvider sqlConnectionStringProvider,
        IAccessTokenHandler accessTokenHandler,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider)
    {
        EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));
        EnsureArg.IsNotNull(accessTokenHandler, nameof(accessTokenHandler));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));

        _sqlConnectionStringProvider = sqlConnectionStringProvider;
        _accessTokenHandler = accessTokenHandler;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
    }


    /// <inheritdoc />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    public async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
    {
        SqlConnection sqlConnection = await SqlConnectionHelper.GetBaseSqlConnectionAsync(
            _sqlConnectionStringProvider,
            _sqlRetryLogicBaseProvider,
            initialCatalog,
            cancellationToken).ConfigureAwait(false);

        // set managed identity access token
        sqlConnection.AccessToken = await _accessTokenHandler.GetAccessTokenAsync(AzureResourceScope, cancellationToken).ConfigureAwait(false);
        return sqlConnection;
    }
}
