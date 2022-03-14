// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Configs;

/// <summary>
/// Options class used to initialize the SqlClient Retry policy
/// </summary>
public class SqlClientRetryOptions
{
    public const string Retry = "Retry";

    public SqlRetryMode Mode { get; set; } = SqlRetryMode.Exponential;

    // Default from https://docs.microsoft.com/en-us/sql/connect/ado-net/configurable-retry-logic-sqlclient-introduction?view=sql-server-ver15
    // Default transient error codes here https://github.com/dotnet/SqlClient/blob/main/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/Reliability/SqlConfigurableRetryFactory.cs
    public SqlRetryLogicOption Settings { get; set; } = new SqlRetryLogicOption
        {
            NumberOfTries = 5,
            DeltaTime = TimeSpan.FromSeconds(1),
            MaxTimeInterval = TimeSpan.FromSeconds(20),
        };
}
