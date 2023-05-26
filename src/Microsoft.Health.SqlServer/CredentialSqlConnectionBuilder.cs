// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

public class CredentialSqlConnectionBuilder : ISqlConnectionBuilder
{
    private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private readonly TokenCredential _tokenCredential;
    private readonly string _azureResource = "https://database.windows.net/.default";

    public CredentialSqlConnectionBuilder(
        ISqlConnectionStringProvider sqlConnectionStringProvider,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider,
        TokenCredential tokenCredential)
    {
        EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));
        EnsureArg.IsNotNull(tokenCredential, nameof(tokenCredential));

        _sqlConnectionStringProvider = sqlConnectionStringProvider;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
        _tokenCredential = tokenCredential;
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

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var token = await _tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { _azureResource }), cancellationToken).ConfigureAwait(false);
        return token.Token;
    }
}
