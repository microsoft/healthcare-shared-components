// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using Azure.Identity;
using EnsureThat;

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
        /// <param name="useManagedIdentity">Should use managed identity.</param>
        /// <returns>SqlConnection object.</returns>
        public static SqlConnection GetSqlConnection(string connectionString, bool useManagedIdentity)
        {
            EnsureArg.IsNotNullOrEmpty(connectionString);

            SqlConnection sqlConnection = new SqlConnection(connectionString);

            if (useManagedIdentity)
            {
                var credential = new DefaultAzureCredential();
                var token = new Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider().GetAccessTokenAsync("https://database.windows.net/").Result;
                sqlConnection.AccessToken = token;
            }

            return sqlConnection;
        }
    }
}
