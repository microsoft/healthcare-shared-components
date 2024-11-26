// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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

namespace Microsoft.Health.SqlServer.Features.Storage;

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

        try
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.SelectCompatibleSchemaVersions.PopulateCommand(sqlCommandWrapper);

                using (var dataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        compatibleVersions = new CompatibleVersions(ConvertToInt(dataReader.GetValue(0)), ConvertToInt(dataReader.GetValue(1)));
                    }
                    else
                    {
                        throw new CompatibleVersionsNotFoundException(Resources.CompatibilityRecordNotFound);
                    }
                }

                return compatibleVersions;
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.IsInvalidAccess())
        {
            _logger.LogError(httpEx, "Error while getting SQL connection on getting latest compatible version");
            throw;
        }

        static int ConvertToInt(object o)
        {
            if (o == DBNull.Value)
            {
                throw new CompatibleVersionsNotFoundException(Resources.CompatibilityRecordNotFound);
            }
            else
            {
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
        }
    }

    public async Task<int> UpsertInstanceSchemaInformationAsync(string name, SchemaInformation schemaInformation, CancellationToken cancellationToken)
    {
        try
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.UpsertInstanceSchema.PopulateCommand(
                     sqlCommandWrapper,
                     name,
                     schemaInformation.MaximumSupportedVersion,
                     schemaInformation.MinimumSupportedVersion,
                     _configuration.SchemaOptions.InstanceRecordExpirationTimeInMinutes);

                return (int)await sqlCommandWrapper.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.IsInvalidAccess())
        {
            _logger.LogError(httpEx, "Error while getting SQL connection on upserting InstanceSchema information");
            throw;
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number == SqlErrorCodes.CouldNotFoundStoredProc && schemaInformation.Current == null)
            {
                // this could happen during schema initialization until base schema is not executed
                throw;
            }

            if (sqlEx.IsCMKError())
            {
                // do not log error if it is a CMK error, since it is the customer's issue and not a system issue
                throw;
            }

            _logger.LogError(sqlEx, "Error from SQL database on upserting InstanceSchema information");
            throw;
        }
    }

    public async Task DeleteExpiredInstanceSchemaAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.DeleteInstanceSchema.PopulateCommand(sqlCommandWrapper);

                await sqlCommandWrapper.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.IsInvalidAccess())
        {
            _logger.LogError(httpEx, "Error while getting SQL connection on deleting expired InstanceSchema records");
            throw;
        }
        catch (SqlException sqlEx)
        {
            _logger.LogError(sqlEx, "Error from SQL database on deleting expired InstanceSchema records");
            throw;
        }
    }

    public async Task<List<CurrentVersionInformation>> GetCurrentVersionAsync(CancellationToken cancellationToken)
    {
        var currentVersions = new List<CurrentVersionInformation>();

        try
        {
            using (SqlConnectionWrapper sqlConnectionWrapper = await _sqlConnectionWrapperFactory.ObtainSqlConnectionWrapperAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            using (SqlCommandWrapper sqlCommandWrapper = sqlConnectionWrapper.CreateRetrySqlCommand())
            {
                SchemaShared.SelectCurrentVersionsInformation.PopulateCommand(sqlCommandWrapper);

                using (var dataReader = await sqlCommandWrapper.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (dataReader.HasRows)
                    {
                        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            IList<string> instanceNames = new List<string>();
                            if (!await dataReader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false))
                            {
                                string names = dataReader.GetString(2);
                                instanceNames = names.Split(",").ToList();
                            }

                            var status = (string)dataReader.GetValue(1);

                            // To combine the complete and completed version since earlier status was marked in 'complete' status and now the fix has made to mark the status in completed state
                            status = string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase) ? "completed" : status;
                            var schemaVersionStatus = Enum.Parse<SchemaVersionStatus>(status, ignoreCase: true);
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
        }
        catch (HttpRequestException httpEx) when (httpEx.IsInvalidAccess())
        {
            _logger.LogError(httpEx, "Error while getting SQL connection on retrieving current version information");
            throw;
        }
        catch (SqlException sqlEx)
        {
            _logger.LogError(sqlEx, "Error from SQL database on retrieving current version information");
            throw;
        }

        return currentVersions;
    }
}
