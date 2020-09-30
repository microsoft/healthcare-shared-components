// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;

namespace Microsoft.Health.SqlServer
{
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Get unopened SqlConnection object.
        /// </summary>
        /// <param name="connectToMaster">Should connect to master database?</param>
        public SqlConnection GetSqlConnection(bool connectToMaster = false);
    }
}
