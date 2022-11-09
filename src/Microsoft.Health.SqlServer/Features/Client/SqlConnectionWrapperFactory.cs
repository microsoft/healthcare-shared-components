// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
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
    private readonly ILoggerFactory _loggerFactory;

    private readonly ILogger<SqlConnectionWrapper> _sqlConnectionWrapperLogger;

    public SqlConnectionWrapperFactory(
        SqlTransactionHandler sqlTransactionHandler,
        ISqlConnectionBuilder sqlConnectionBuilder,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider,
        IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration,
        ILoggerFactory loggerFactory)
    {
        _sqlTransactionHandler = EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
        _sqlConnectionBuilder = EnsureArg.IsNotNull(sqlConnectionBuilder, nameof(sqlConnectionBuilder));
        _sqlRetryLogicBaseProvider = EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));
        _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
        _loggerFactory = EnsureArg.IsNotNull(loggerFactory, nameof(loggerFactory));
        _sqlConnectionWrapperLogger = _loggerFactory.CreateLogger<SqlConnectionWrapper>();
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    public async Task<SqlConnectionWrapper> ObtainSqlConnectionWrapperAsync(CancellationToken cancellationToken, bool enlistInTransaction = false)
    {
        var sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlConnectionBuilder, _sqlRetryLogicBaseProvider, enlistInTransaction, _sqlServerDataStoreConfiguration, _sqlConnectionWrapperLogger);
        await sqlConnectionWrapper.InitializeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return sqlConnectionWrapper;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Callers are responsible for disposal.")]
    public async Task<SqlConnectionWrapper> ObtainSqlConnectionWrapperAsync(string initialCatalog, CancellationToken cancellationToken, bool enlistInTransaction = false)
    {
        var sqlConnectionWrapper = new SqlConnectionWrapper(_sqlTransactionHandler, _sqlConnectionBuilder, _sqlRetryLogicBaseProvider, enlistInTransaction, _sqlServerDataStoreConfiguration, _sqlConnectionWrapperLogger);
        await sqlConnectionWrapper.InitializeAsync(initialCatalog, cancellationToken: cancellationToken).ConfigureAwait(false);

        return sqlConnectionWrapper;
    }
}
