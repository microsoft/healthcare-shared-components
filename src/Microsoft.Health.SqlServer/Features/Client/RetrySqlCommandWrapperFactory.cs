// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Features.Client
{
    /// <summary>
    /// Provides functionality to create an instance of <see cref="RetrySqlCommandWrapper"/>.
    /// </summary>
    internal class RetrySqlCommandWrapperFactory : SqlCommandWrapperFactory
    {
        private readonly SqlRetryLogicBaseProvider _sqlRetryLogicBaseProvider;

        public RetrySqlCommandWrapperFactory(SqlRetryLogicBaseProvider sqlRetryLogicBaseProvider)
        {
            EnsureArg.IsNotNull(sqlRetryLogicBaseProvider, nameof(sqlRetryLogicBaseProvider));
            _sqlRetryLogicBaseProvider = sqlRetryLogicBaseProvider;
        }

        /// <inheritdoc/>
        public override SqlCommandWrapper Create(SqlCommand sqlCommand)
        {
            return new RetrySqlCommandWrapper(
                base.Create(sqlCommand),
                _sqlRetryLogicBaseProvider);
        }
    }
}
