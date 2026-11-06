// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Health.SqlServer.Configs;

namespace Microsoft.Health.SqlServer;

internal sealed class ScopedSqlConnectionBuilder : DefaultSqlConnectionBuilder
{
    public ScopedSqlConnectionBuilder(IOptionsSnapshot<SqlServerDataStoreConfiguration> options, SqlRetryLogicBaseProvider retryProvider)
        : base(options, retryProvider)
    {
    }
}
