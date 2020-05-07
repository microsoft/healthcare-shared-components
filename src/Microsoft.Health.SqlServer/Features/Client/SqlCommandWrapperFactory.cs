// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;
using EnsureThat;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality to create an instance of <see cref="SqlCommandWrapper"/>.
    /// </summary>
    public class SqlCommandWrapperFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="SqlCommandWrapper"/>.
        /// </summary>
        /// <param name="sqlCommand">The underlying <see cref="SqlCommand"/> instance.</param>
        /// <returns>A newly created <see cref="SqlCommandWrapper"/>.</returns>
        public virtual SqlCommandWrapper Create(SqlCommand sqlCommand)
        {
            EnsureArg.IsNotNull(sqlCommand, nameof(sqlCommand));

            return new SqlCommandWrapper(sqlCommand);
        }
    }
}
