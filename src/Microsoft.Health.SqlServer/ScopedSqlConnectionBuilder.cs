// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer;

public class ScopedSqlConnectionBuilder : ISqlConnectionBuilder
{
    public SqlConnection CreateConnection(Action<SqlConnectionStringBuilder> configure = null)
    { }
}
