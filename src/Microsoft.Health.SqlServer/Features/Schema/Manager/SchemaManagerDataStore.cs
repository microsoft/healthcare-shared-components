// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Core;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Extensions;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public class SchemaManagerDataStore : ISchemaManagerDataStore
{
    private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
    private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;
    private readonly ILogger<SchemaManagerDataStore> _logger;

    public SchemaManagerDataStore(
        SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
        IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration,
        ILogger<SchemaManagerDataStore> logger)
    {
        _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
        _sqlConnectionWrapperFactory = EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
        _logger = EnsureArg.IsNotNull(logger, nameof(logger));
    }

    /// <inheritdoc />
    public async Task ExecuteScriptAndCompleteSchemaVersionAsync(string script, int version, bool applyFullSchemaSnapshot, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(script, nameof(script));
        EnsureArg.IsGte(version, 1);

        using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var serverConnection = GetServerConnectionWithTimeout(sqlCommandWrapper.Connection);

            try
            {
                // FullSchemaSnapshot script(x.sql) inserts 'started' status into the SchemaVersion table itself.
                if (!applyFullSchemaSnapshot)
                {
                    await UpsertSchemaVersionAsync(sqlCommandWrapper.Connection, version, SchemaVersionStatus.started.ToString(), cancellationToken).ConfigureAwait(false);
                }

                var server = new Server(serverConnection);
                var watch = Stopwatch.StartNew();
                _logger.LogInformation("Script execution started at {UtcTime}", Clock.UtcNow);

                server.ConnectionContext.ExecuteNonQuery(script);

                watch.Stop();
                _logger.LogInformation("Script execution time is {ElapsedTime}", watch.Elapsed);
                await UpsertSchemaVersionAsync(sqlCommandWrapper.Connection, version, SchemaVersionStatus.completed.ToString(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (e is SqlException || e is ExecutionFailureException)
            {
                await UpsertSchemaVersionAsync(sqlCommandWrapper.Connection, version, SchemaVersionStatus.failed.ToString(), cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task DeleteSchemaVersionAsync(int version, string status, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(status, nameof(status));
        EnsureArg.IsGte(version, 1);

        using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            sqlCommandWrapper.CommandText = "DELETE FROM dbo.SchemaVersion WHERE Version = @version AND Status = @status";
            sqlCommandWrapper.Parameters.AddWithValue("@version", version);
            sqlCommandWrapper.Parameters.AddWithValue("@status", status);

            await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCurrentSchemaVersionAsync(CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            sqlCommandWrapper.CommandType = CommandType.StoredProcedure;
            sqlCommandWrapper.CommandText = "dbo.SelectCurrentSchemaVersion";

            object current = await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return (current == null || Convert.IsDBNull(current)) ? 0 : (int)current;
        }
    }

    private static async Task UpsertSchemaVersionAsync(SqlConnection connection, int version, string status, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(connection, nameof(connection));
        EnsureArg.IsNotNull(status, nameof(status));
        EnsureArg.IsGte(version, 1);

        using var upsertCommand = new SqlCommand("dbo.UpsertSchemaVersion", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        upsertCommand.Parameters.AddWithValue("@version", version);
        upsertCommand.Parameters.AddWithValue("@status", status);

        await upsertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ExecuteScriptAsync(string script, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(script, nameof(script));

        using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            var server = new Server(GetServerConnectionWithTimeout(sqlCommandWrapper.Connection));
            server.ConnectionContext.ExecuteNonQuery(script);
        }
    }

    /// <inheritdoc />
    public async Task<bool> BaseSchemaExistsAsync(CancellationToken cancellationToken)
    {
        return await ObjectExistsAsync("SelectCurrentVersionsInformation", "P", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ObjectExistsAsync(string objectName, string objectType, CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            sqlCommandWrapper.CommandText = "SELECT COUNT(*) FROM sys.objects WHERE name = @name and type = @type";
            sqlCommandWrapper.Parameters.AddWithValue("@name", objectName);
            sqlCommandWrapper.Parameters.AddWithValue("@type", objectType);

            return (int)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> InstanceSchemaRecordExistsAsync(CancellationToken cancellationToken)
    {
        using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
        using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
        {
            sqlCommandWrapper.CommandText = "SELECT COUNT(*) FROM dbo.InstanceSchema";

            return (int)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != 0;
        }
    }

    private ServerConnection GetServerConnectionWithTimeout(SqlConnection sqlConnection)
    {
        var serverConnection = new ServerConnection(sqlConnection);
        serverConnection.StatementTimeout = (int)_sqlServerDataStoreConfiguration.StatementTimeout.TotalSeconds;
        _logger.LogInformation("ServerConnection timeout sets to {TimeoutSeconds} seconds", serverConnection.StatementTimeout);
        return serverConnection;
    }
}
