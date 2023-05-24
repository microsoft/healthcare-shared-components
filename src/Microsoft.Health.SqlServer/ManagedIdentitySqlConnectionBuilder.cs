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
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private readonly IAzureTokenCredentialProvider _azureTokenCredentialProvider;

    public ManagedIdentitySqlConnectionBuilder(
        IAzureTokenCredentialProvider azureTokenCredentialProvider,
        ISqlConnectionStringProvider sqlConnectionStringProvider,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider)
    {
        EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));
        EnsureArg.IsNotNull(azureTokenCredentialProvider, nameof(azureTokenCredentialProvider));

        _sqlConnectionStringProvider = sqlConnectionStringProvider;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
        _azureTokenCredentialProvider = azureTokenCredentialProvider;
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
        sqlConnection.AccessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        return sqlConnection;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return await _azureTokenCredentialProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
    }
}
