// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Client;

public class SqlConnectionWrapper : IDisposable
{
    private readonly bool _enlistInTransactionIfPresent;
    private readonly SqlTransactionHandler _sqlTransactionHandler;
    private readonly ISqlConnectionBuilder _sqlConnectionBuilder;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
    private SqlConnection _sqlConnection;
    private SqlTransaction _sqlTransaction;

    internal SqlConnectionWrapper(
        SqlTransactionHandler sqlTransactionHandler,
        ISqlConnectionBuilder connectionBuilder,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider,
        bool enlistInTransactionIfPresent,
        SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
    {
        EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
        EnsureArg.IsNotNull(connectionBuilder, nameof(connectionBuilder));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));
        EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

        _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

        _sqlTransactionHandler = sqlTransactionHandler;
        _enlistInTransactionIfPresent = enlistInTransactionIfPresent;
        _sqlConnectionBuilder = connectionBuilder;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
    }

    public SqlConnection SqlConnection
    {
        get { return _sqlConnection; }
    }

    public SqlTransaction SqlTransaction
    {
        get
        {
            return _sqlTransaction;
        }
    }

    internal async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope?.SqlConnection != null)
        {
            _sqlConnection = _sqlTransactionHandler.SqlTransactionScope.SqlConnection;
        }
        else
        {
            _sqlConnection = await _sqlConnectionBuilder.GetSqlConnectionAsync(cancellationToken: cancellationToken);
        }

        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope != null && _sqlTransactionHandler.SqlTransactionScope.SqlConnection == null)
        {
            _sqlTransactionHandler.SqlTransactionScope.SqlConnection = SqlConnection;
        }

        if (SqlConnection.State != ConnectionState.Open)
        {
            await SqlConnection.OpenAsync(cancellationToken);
        }

        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope != null)
        {
            _sqlTransaction = _sqlTransactionHandler.SqlTransactionScope.SqlTransaction ?? SqlConnection.BeginTransaction();

            if (_sqlTransactionHandler.SqlTransactionScope.SqlTransaction == null)
            {
                _sqlTransactionHandler.SqlTransactionScope.SqlTransaction = SqlTransaction;
            }
        }
    }

    [Obsolete("Please use " + nameof(CreateRetrySqlCommand) + " or " + nameof(CreateNonRetrySqlCommand) + " instead.")]
    public SqlCommandWrapper CreateSqlCommand()
    {
        return CreateRetrySqlCommand();
    }

    /// <summary>
    /// Sql statements that are idempotent should get this SqlCommand which retries on transient failures.
    /// </summary>
    /// <returns>The <see cref="SqlCommandWrapper"/></returns>
    public SqlCommandWrapper CreateRetrySqlCommand()
    {
        SqlCommand sqlCommand = SqlConnection.CreateCommand();
        sqlCommand.CommandTimeout = (int)_sqlServerDataStoreConfiguration.CommandTimeout.TotalSeconds;
        sqlCommand.Transaction = SqlTransaction;
        sqlCommand.RetryLogicProvider = _sqlRetryLogicBaseProvider;
        return new SqlCommandWrapper(sqlCommand);
    }

    /// <summary>
    /// Sql statements that cannot be retried should get this SqlCommand
    /// </summary>
    /// <returns>The <see cref="SqlCommandWrapper"/></returns>
    public SqlCommandWrapper CreateNonRetrySqlCommand()
    {
        SqlCommand sqlCommand = SqlConnection.CreateCommand();
        sqlCommand.CommandTimeout = (int)_sqlServerDataStoreConfiguration.CommandTimeout.TotalSeconds;
        sqlCommand.Transaction = SqlTransaction;
        return new SqlCommandWrapper(sqlCommand);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_enlistInTransactionIfPresent || _sqlTransactionHandler.SqlTransactionScope == null)
            {
                SqlConnection?.Dispose();
                SqlTransaction?.Dispose();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
