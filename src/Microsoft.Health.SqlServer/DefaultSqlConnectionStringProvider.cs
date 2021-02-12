// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
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
        private readonly SqlServerDataStoreConfiguration _sqlServerDataStoreConfiguration;

        public DefaultSqlConnectionStringProvider(SqlServerDataStoreConfiguration sqlServerDataStoreConfiguration)
        {
            _sqlServerDataStoreConfiguration = EnsureArg.IsNotNull(sqlServerDataStoreConfiguration, nameof(sqlServerDataStoreConfiguration));
        }

        /// <inheritdoc />
        public Task<string> GetSqlConnectionString(CancellationToken cancellationToken)
        {
            return Task.FromResult(_sqlServerDataStoreConfiguration.ConnectionString);
        }
    }
}