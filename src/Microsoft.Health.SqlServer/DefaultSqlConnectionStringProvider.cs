// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer
{
    /// <summary>
    /// The default SQL connection string provider uses the connection string specified in configuration.
    /// </summary>
    public class DefaultSqlConnectionStringProvider : ISqlConnectionStringProvider
    {
        private readonly string _sqlConnectionString;

        public DefaultSqlConnectionStringProvider(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
        {
            EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
            _sqlConnectionString = sqlServerDataStoreConfiguration.ConnectionString;
        }

        /// <inheritdoc />
        public Task<string> GetSqlConnectionString()
        {
            return Task.FromResult(_sqlConnectionString);
        }
    }
}