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
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Caller owns the disposal of the object")]
    private readonly SqlTransactionHandler _sqlTransactionHandler;
    private readonly ISqlConnectionBuilder _sqlConnectionBuilder;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private readonly SqlServerDataStoreOptions _options;

    internal SqlConnectionWrapper(
        SqlTransactionHandler sqlTransactionHandler,
        ISqlConnectionBuilder connectionBuilder,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider,
        bool enlistInTransactionIfPresent,
        SqlServerDataStoreOptions options)
    {
        _options = EnsureArg.IsNotNull(options, nameof(options));
        _sqlTransactionHandler = EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
        _enlistInTransactionIfPresent = enlistInTransactionIfPresent;
        _sqlConnectionBuilder = EnsureArg.IsNotNull(connectionBuilder, nameof(connectionBuilder));
        _sqlRetryLogicBaseProvider = EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));
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
            SqlConnection = _sqlConnectionBuilder.CreateConnection(b =>
            {
                if (_options.MaxPoolSize.HasValue)
                    b.MaxPoolSize = _options.MaxPoolSize.GetValueOrDefault();

                configure?.Invoke(b);
            });
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

    /// <summary>
    /// Sql statements that are idempotent should get this SqlCommand which retries on transient failures.
    /// </summary>
    /// <returns>The <see cref="SqlCommandWrapper"/></returns>
    public SqlCommandWrapper CreateRetrySqlCommand()
    {
        SqlCommand sqlCommand = SqlConnection.CreateCommand();
        sqlCommand.CommandTimeout = (int)_options.CommandTimeout.TotalSeconds;
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
        sqlCommand.CommandTimeout = (int)_options.CommandTimeout.TotalSeconds;
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
