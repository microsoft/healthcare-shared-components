// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using EnsureThat;
using Microsoft.Azure.Services.AppAuthentication;

namespace Microsoft.Health.SqlServer
{
    public static class SqlConnectionHelper
    {
        /// <summary>
        /// Get unopened SqlConnection object.
        /// If useManagedIdentity is set to true, access token will be requested
        /// and respective property will be set in SqlConnection.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <returns>SqlConnection object.</returns>
        public static SqlConnection GetSqlConnectionAsync(string connectionString)
        {
            EnsureArg.IsNotNullOrEmpty(connectionString);

            SqlConnection sqlConnection = new SqlConnection(connectionString);

            var token = new AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/").Result;
            sqlConnection.AccessToken = token;
            return sqlConnection;
        }
    }
}