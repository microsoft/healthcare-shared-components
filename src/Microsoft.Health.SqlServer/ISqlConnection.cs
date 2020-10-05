// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer
{
    public interface ISqlConnection
    {
        /// <summary>
        /// Get unopened SqlConnection object.
        /// Initial catalog is determined from the connection string.
        /// </summary>
        /// <returns>SqlConnection object.</returns>
        public SqlConnection GetSqlConnection();

        /// <summary>
        /// Get unopened SqlConnection object.
        /// Initial catalog is set based on the passed in parameter.
        /// </summary>
        /// <param name="initialCatalog">Initial catalog to connect to.</param>
        public SqlConnection GetSqlConnection(string initialCatalog);
    }
}
