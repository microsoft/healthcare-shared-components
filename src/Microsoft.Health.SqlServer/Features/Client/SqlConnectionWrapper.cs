// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
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

        if (_sqlServerDataStoreConfiguration.MaxPoolSize.HasValue)
        {
            EnsureArg.IsGt(_sqlServerDataStoreConfiguration.MaxPoolSize.Value, 0, nameof(_sqlServerDataStoreConfiguration.MaxPoolSize));
            EnsureArg.IsLt(_sqlServerDataStoreConfiguration.MaxPoolSize.Value, SqlConstants.MaxPoolSizeLimit, nameof(_sqlServerDataStoreConfiguration.MaxPoolSize));
        }

        _sqlTransactionHandler = sqlTransactionHandler;
        _enlistInTransactionIfPresent = enlistInTransactionIfPresent;
        _sqlConnectionBuilder = connectionBuilder;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
    }

    public SqlConnection SqlConnection { get; private set; }

    public SqlTransaction SqlTransaction { get; private set; }

    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "BeginTransactionAsync is only implemented by base class.")]
    internal async Task InitializeAsync(Action<SqlConnectionStringBuilder> configure = null, CancellationToken cancellationToken = default)
    {
        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope?.SqlConnection != null)
        {
            SqlConnection = _sqlTransactionHandler.SqlTransactionScope.SqlConnection;
        }
        else
        {
            Action<SqlConnectionStringBuilder> configureBuilder = b =>
            {
                if (_sqlServerDataStoreConfiguration.MaxPoolSize.HasValue)
                    b.MaxPoolSize = _sqlServerDataStoreConfiguration.MaxPoolSize.GetValueOrDefault();

                configure?.Invoke(b);
            };

            SqlConnection = await _sqlConnectionBuilder.CreateConnectionAsync(configureBuilder, cancellationToken).ConfigureAwait(false);
        }

        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope != null && _sqlTransactionHandler.SqlTransactionScope.SqlConnection == null)
        {
            _sqlTransactionHandler.SqlTransactionScope.SqlConnection = SqlConnection;
        }

        if (SqlConnection.State != ConnectionState.Open)
        {
            await SqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope != null)
        {
            SqlTransaction = _sqlTransactionHandler.SqlTransactionScope.SqlTransaction ?? SqlConnection.BeginTransaction();

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
