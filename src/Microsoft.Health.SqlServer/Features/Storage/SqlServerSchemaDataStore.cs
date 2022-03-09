// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Exceptions;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.SqlServer.Features.Storage
{
    internal class SqlServerSchemaDataStore : ISchemaDataStore
    {
        private readonly SqlConnectionWrapperFactory _sqlConnectionWrapperFactory;
        private readonly SqlServerDataStoreConfiguration _configuration;
        private readonly ILogger<SqlServerSchemaDataStore> _logger;

        public SqlServerSchemaDataStore(
            SqlConnectionWrapperFactory sqlConnectionWrapperFactory,
            IOptions<SqlServerDataStoreConfiguration> sqlServerDataStoreConfiguration,
            ILogger<SqlServerSchemaDataStore> logger)
        {
            EnsureArg.IsNotNull(sqlConnectionWrapperFactory, nameof(sqlConnectionWrapperFactory));
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration?.Value, nameof(sqlServerDataStoreConfiguration));
            EnsureArg.IsNotNull(logger, nameof(logger));

            _sqlConnectionWrapperFactory = sqlConnectionWrapperFactory;
            _configuration = sqlServerDataStoreConfiguration.Value;
            _logger = logger;
        }

        public async Task<CompatibleVersions> GetLatestCompatibleVersionsAsync(CancellationToken cancellationToken)
        {
            CompatibleVersions compatibleVersions;
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.SelectCompatibleSchemaVersions.PopulateCommand(sqlCommandWrapper);

                using (var dataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken))
                {
                    if (await dataReader.ReadAsync(cancellationToken))
                    {
                        compatibleVersions = new CompatibleVersions(ConvertToInt(dataReader.GetValue(0)), ConvertToInt(dataReader.GetValue(1)));
                    }
                    else
                    {
                        throw new SqlRecordNotFoundException(Resources.CompatibilityRecordNotFound);
                    }
                }

                return compatibleVersions;
            }

            int ConvertToInt(object o)
            {
                if (o == DBNull.Value)
                {
                    throw new SqlRecordNotFoundException(Resources.CompatibilityRecordNotFound);
                }
                else
                {
                    return Convert.ToInt32(o);
                }
            }
        }

        public async Task<int> UpsertInstanceSchemaInformationAsync(string name, SchemaInformation schemaInformation, CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.UpsertInstanceSchema.PopulateCommand(
                     sqlCommandWrapper,
                     name,
                     schemaInformation.MaximumSupportedVersion,
                     schemaInformation.MinimumSupportedVersion,
                     _configuration.SchemaOptions.InstanceRecordExpirationTimeInMinutes);
                try
                {
                    return (int)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken);
                }
                catch (SqlException e)
                {
                    _logger.LogError(e, "Error from SQL database on upserting InstanceSchema information");
                    throw;
                }
            }
        }

        public async Task DeleteExpiredInstanceSchemaAsync(CancellationToken cancellationToken)
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.DeleteInstanceSchema.PopulateCommand(sqlCommandWrapper);
                try
                {
                    await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqlException e)
                {
                    _logger.LogError(e, "Error from SQL database on deleting expired InstanceSchema records");
                    throw;
                }
            }
        }

        public async Task<List<CurrentVersionInformation>> GetCurrentVersionAsync(CancellationToken cancellationToken)
        {
            var currentVersions = new List<CurrentVersionInformation>();
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.SelectCurrentVersionsInformation.PopulateCommand(sqlCommandWrapper);

                try
                {
                    using (var dataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken))
                    {
                        if (dataReader.HasRows)
                        {
                            while (await dataReader.ReadAsync(cancellationToken))
                            {
                                IList<string> instanceNames = new List<string>();
                                if (!await dataReader.IsDBNullAsync(2, cancellationToken))
                                {
                                    string names = dataReader.GetString(2);
                                    instanceNames = names.Split(",").ToList();
                                }

                                var status = (string)dataReader.GetValue(1);

                                // To combine the complete and completed version since earlier status was marked in 'complete' status and now the fix has made to mark the status in completed state
                                status = string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase) ? "completed" : status;
                                var schemaVersionStatus = (SchemaVersionStatus)Enum.Parse(typeof(SchemaVersionStatus), status, true);
                                var currentVersion = new CurrentVersionInformation((int)dataReader.GetValue(0), schemaVersionStatus, instanceNames);
                                currentVersions.Add(currentVersion);
                            }
                        }
                        else
                        {
                            return currentVersions;
                        }
                    }
                }
                catch (SqlException e)
                {
                    _logger.LogError(e, "Error from SQL database on retrieving current version information");
                    throw;
                }
            }

            return currentVersions;
        }
    }
}
