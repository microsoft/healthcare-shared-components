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
using Microsoft.Extensions.Logging;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Storage;
using Polly;

namespace Microsoft.Health.SqlServer.Features.Client;

public class SqlConnectionWrapper : IDisposable
{
    private readonly bool _enlistInTransactionIfPresent;
    private readonly SqlTransactionHandler _sqlTransactionHandler;
    private readonly ISqlConnectionBuilder _sqlConnectionBuilder;
    private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
    private readonly ILogger<SqlConnectionWrapper> _logger;

    private const int RetryAttempts = 3;
    private static readonly TimeSpan RetrySleepDuration = TimeSpan.FromSeconds(2);

    internal SqlConnectionWrapper(
        SqlTransactionHandler sqlTransactionHandler,
        ISqlConnectionBuilder connectionBuilder,
        SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider,
        bool enlistInTransactionIfPresent,
        SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration,
        ILogger<SqlConnectionWrapper> logger)
    {
        EnsureArg.IsNotNull(sqlTransactionHandler, nameof(sqlTransactionHandler));
        EnsureArg.IsNotNull(connectionBuilder, nameof(connectionBuilder));
        EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));
        EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
        EnsureArg.IsNotNull(logger, nameof(logger));

        _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));

        _sqlTransactionHandler = sqlTransactionHandler;
        _enlistInTransactionIfPresent = enlistInTransactionIfPresent;
        _sqlConnectionBuilder = connectionBuilder;
        _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
        _logger = logger;
    }

    public SqlConnection SqlConnection { get; private set; }

    public SqlTransaction SqlTransaction { get; private set; }

    internal async Task InitializeAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
    {
        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope?.SqlConnection != null)
        {
            SqlConnection = _sqlTransactionHandler.SqlTransactionScope.SqlConnection;
        }
        else
        {
            SqlConnection = await _sqlConnectionBuilder.GetSqlConnectionAsync(initialCatalog, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (_enlistInTransactionIfPresent && _sqlTransactionHandler.SqlTransactionScope != null && _sqlTransactionHandler.SqlTransactionScope.SqlConnection == null)
        {
            _sqlTransactionHandler.SqlTransactionScope.SqlConnection = SqlConnection;
        }

        if (SqlConnection.State != ConnectionState.Open)
        {
            await OpenRetriableSqlConnectionAsync(cancellationToken).ConfigureAwait(false);
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
    /// Open sql connection which is retried up to 3 times if <see cref="SqlException"/> is caught with state 35.
    /// <remarks>
    /// Retries have been added to cater to the following exception:
    /// "A connection was successfully established with the server, but then an error occurred during the login process.
    /// (provider: TCP Provider, error: 35 - An internal exception was caught)"
    /// </remarks>
    /// </summary>
    /// <param name="cancellationToken">Cancellation Token.</param>
    private async Task OpenRetriableSqlConnectionAsync(CancellationToken cancellationToken)
    {
        int attempts = 1;

        await Policy.Handle<SqlException>(se => se.State == 35)
        .WaitAndRetryAsync(
            retryCount: RetryAttempts,
            sleepDurationProvider: (retryCount) => RetrySleepDuration,
            onRetry: (exception, retryCount) =>
            {
                _logger.LogWarning(
                    exception,
                    "Attempt {Attempt} of {MaxAttempts} to open Sql connection.",
                    attempts++,
                    RetryAttempts);
            })
        .ExecuteAsync(token => SqlConnection.OpenAsync(token), cancellationToken).ConfigureAwait(false);
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
