// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client;

public class SqlConnectionWrapperFactory
{
    private readonly SqlTransactionHandler _sqlTransactionHandler;
    private readonly ISqlConnectionBuilder _sqlConnectionBuilder;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

    public SqlConnectionWrapperFactory(
        SqlTransactionHandler sqlTransactionHandler,
        ISqlConnectionBuilder sqlConnectionBuilder,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider,
        IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration)
    {
        EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
        EnsureArg.IsNotNull(sqlConnectionBuilder, nameof(sqlConnectionBuilder));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));

        _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
        _sqlTransactionHandler = sqlTransactionHandler;
        _sqlConnectionBuilder = sqlConnectionBuilder;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
    }

    public string DefaultDatabase => _sqlConnectionBuilder.DefaultDatabase;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    public virtual async Task<SqlConnectionWrapper> GetConnectionWrapperAsync(Action<SqlConnectionStringBuilder> configure = null, bool enlistInTransaction = false, CancellationToken cancellationToken = default)
    {
        var sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlConnectionBuilder, _sqlRetryLogicBaseProvider, enlistInTransaction, _sqlServerDataStoreConfiguration);
        await sqlConnectionWrapper.InitializeAsync(configure, cancellationToken: cancellationToken).ConfigureAwait(false);

        return sqlConnectionWrapper;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    public virtual async Task<SqlConnectionWrapper> ObtainSqlConnectionWrapperAsync(CancellationToken cancellationToken, bool enlistInTransaction = false)
    {
        var sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlConnectionBuilder, _sqlRetryLogicBaseProvider, enlistInTransaction, _sqlServerDataStoreConfiguration);
        await sqlConnectionWrapper.InitializeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return sqlConnectionWrapper;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    public async Task<SqlConnectionWrapper> ObtainSqlConnectionWrapperAsync(string initialCatalog, CancellationToken cancellationToken, bool enlistInTransaction = false)
    {
        var sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlConnectionBuilder, _sqlRetryLogicBaseProvider, enlistInTransaction, _sqlServerDataStoreConfiguration);
        await sqlConnectionWrapper.InitializeAsync(
            initialCatalog is not null ? b => b.InitialCatalog = initialCatalog : null,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return sqlConnectionWrapper;
    }
}
