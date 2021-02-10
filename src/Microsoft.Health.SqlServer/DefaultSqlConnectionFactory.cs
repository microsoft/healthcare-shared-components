// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer
{
    /// <summary>
    /// Default Sql Connection Factory is responsible to handle Sql connections that can be made purely based on connection string.
    /// Connection string containing user name and password, or integrated auth are perfect examples for this.
    /// </summary>
    public class DefaultSqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;

        public DefaultSqlConnectionFactory(ISqlConnectionStringProvider sqlConnectionStringProvider)
        {
            EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));

            _sqlConnectionStringProvider = sqlConnectionStringProvider;
        }

        /// <inheritdoc />
        public async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
        {
            SqlConnection sqlConnection;
            string sqlConnectionString = await _sqlConnectionStringProvider.GetSqlConnectionString();

            if (initialCatalog == null)
            {
                sqlConnection = new SqlConnection(sqlConnectionString);
            }
            else
            {
                SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(sqlConnectionString) { InitialCatalog = initialCatalog };
                sqlConnection = new SqlConnection(connectionBuilder.ToString());
            }

            return sqlConnection;
        }
    }
}
