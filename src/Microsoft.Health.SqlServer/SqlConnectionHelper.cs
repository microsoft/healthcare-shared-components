// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer
{
    /// <summary>
    /// Helper class to build the base <see cref="SqlConnection"/> object
    /// </summary>
    /// Could not do a real builder class because SqlConnection is of type IDisposable and it cannot be a member, since its disposal is handled in SqlConnectionWrapper
    internal class SqlConnectionHelper
    {
        /// <summary>
        /// Get the SqlConnection object with right connection properties to retry
        /// </summary>
        /// <param name="sqlConnectionStringProvider">sqlConnectionStringProvider</param>
        /// <param name="sqlRetryLogic"><see cref="SqlRetryLogicBaseProvider"/>.</param>
        /// <param name="initialCatalog">initialCatalog</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns>SqlConnection</returns>
        /// <exception cref="InvalidOperationException">Empty sql connection string</exception>
        internal static async Task<SqlConnection> GetBaseSqlConnectionAsync(
            ISqlConnectionStringProvider sqlConnectionStringProvider,
            SqlRetryLogicBaseProvider sqlRetryLogic,
            string initialCatalog = null,
            CancellationToken cancellationToken = default)
        {
            string sqlConnectionString = await sqlConnectionStringProvider.GetSqlConnectionString(cancellationToken);
            if (string.IsNullOrEmpty(sqlConnectionString))
            {
                throw new InvalidOperationException("The SQL connection string cannot be null or empty.");
            }

            var connectionStringBuilder = new SqlConnectionStringBuilder(sqlConnectionString);

            if (initialCatalog != null)
            {
                connectionStringBuilder.InitialCatalog = initialCatalog;
            }

            var sqlConnection = new SqlConnection(connectionStringBuilder.ToString())
                {
                    RetryLogicProvider = sqlRetryLogic,
                };
            return sqlConnection;
        }
    }
}
