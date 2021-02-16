// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer
{
    public class ManagedIdentitySqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly ISqlConnectionStringProvider _sqlConnectionStringProvider;
        private readonly IAccessTokenHandler _accessTokenHandler;
        private readonly string _azureResource = "https://database.windows.net/";

        public ManagedIdentitySqlConnectionFactory(ISqlConnectionStringProvider sqlConnectionStringProvider, IAccessTokenHandler accessTokenHandler)
        {
            EnsureArg.IsNotNull(sqlConnectionStringProvider, nameof(sqlConnectionStringProvider));
            EnsureArg.IsNotNull(accessTokenHandler, nameof(accessTokenHandler));

            _sqlConnectionStringProvider = sqlConnectionStringProvider;
            _accessTokenHandler = accessTokenHandler;
        }

        /// <inheritdoc />
        public async Task<SqlConnection> GetSqlConnectionAsync(string initialCatalog = null, CancellationToken cancellationToken = default)
        {
            SqlConnection sqlConnection;
            string sqlConnectionString = await _sqlConnectionStringProvider.GetSqlConnectionString(cancellationToken);
            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                throw new InvalidOperationException("The SQL connection string cannot be null or empty.");
            }

            if (initialCatalog == null)
            {
                sqlConnection = new SqlConnection(sqlConnectionString);
            }
            else
            {
                SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(sqlConnectionString) { InitialCatalog = initialCatalog };
                sqlConnection = new SqlConnection(connectionBuilder.ToString());
            }

            var result = await _accessTokenHandler.GetAccessTokenAsync(_azureResource, cancellationToken);
            sqlConnection.AccessToken = result;

            return sqlConnection;
        }
    }
}
