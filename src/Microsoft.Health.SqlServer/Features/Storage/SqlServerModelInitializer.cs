// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Abstractions.Exceptions;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.SqlServer.Features.Storage
{
    public abstract class SqlServerModelInitializer : IDisposable
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly RetryableInitializationOperation _initializationOperation;

        public SqlServerModelInitializer(SqlServerDataStoreConfiguration configuration, SchemaInformation schemaInformation, ILogger logger)
        {
            EnsureArg.IsNotNull(configuration, nameof(configuration));
            EnsureArg.IsNotNull(schemaInformation, nameof(schemaInformation));
            EnsureArg.IsNotNull(logger, nameof(logger));

            SqlServerDataStoreConfiguration = configuration;
            SchemaInformation = schemaInformation;
            _logger = logger;

            _initializationOperation = new RetryableInitializationOperation(() =>
            {
                if (!SchemaInformation.Current.HasValue)
                {
                    _logger.LogError($"The current version of the database is not available. Unable in initialize {nameof(SqlServerModelInitializer)}.");
                    throw new ServiceUnavailableException();
                }

                return Initialize();
            });

            if (SchemaInformation.Current != null)
            {
                // kick off initialization so that it can be ready for requests. Errors will be observed by requests when they call the method.
                EnsureInitialized();
            }
        }

        protected SqlServerDataStoreConfiguration SqlServerDataStoreConfiguration { get; }

        protected SchemaInformation SchemaInformation { get; }

        public ValueTask EnsureInitialized() => _initializationOperation.EnsureInitialized();

        protected abstract Task Initialize();

        protected int GetStringId(ConcurrentDictionary<string, int> cache, string stringValue, Table table, Column<int> idColumn, Column<string> stringColumn)
        {
            if (cache.TryGetValue(stringValue, out int id))
            {
                return id;
            }

            _logger.LogInformation("Cache miss for string ID on {table}", table);

            using (var connection = new SqlConnection(SqlServerDataStoreConfiguration.ConnectionString))
            {
                connection.Open();

                using (SqlCommand sqlCommand = connection.CreateCommand())
                {
                    sqlCommand.CommandText = $@"
                        SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
                        BEGIN TRANSACTION

                        DECLARE @id int = (SELECT {idColumn} FROM {table} WITH (UPDLOCK) WHERE {stringColumn} = @stringValue)

                        IF (@id IS NULL) BEGIN
                            INSERT INTO {table} 
                                ({stringColumn})
                            VALUES 
                                (@stringValue)
                            SET @id = SCOPE_IDENTITY()
                        END

                        COMMIT TRANSACTION

                        SELECT @id";

                    sqlCommand.Parameters.AddWithValue("@stringValue", stringValue);

                    id = (int)sqlCommand.ExecuteScalar();

                    cache.TryAdd(stringValue, id);
                    return id;
                }
            }
        }

        protected void ThrowIfNotInitialized()
        {
            if (!_initializationOperation.IsInitialized)
            {
                _logger.LogError($"The {nameof(SqlServerModelInitializer)} instance has not been initialized.");
                throw new ServiceUnavailableException();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _initializationOperation?.Dispose();
            }

            _disposed = true;
        }
    }
}