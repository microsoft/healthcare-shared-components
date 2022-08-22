// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Tools.Sql.Tasks.Helpers;

internal static class SqlGenConstants
{
    public const string GeneratedHeader = @"
/*************************************************************************************************
    Auto-Generated from Sql build task. Do not manually edit it. 
**************************************************************************************************/";

    public const string BeginTransaction = "BEGIN TRAN";
    public const string CommitTransaction = "COMMIT";
    public const string Go = "GO";
    public const string SetXabortOn = "SET XACT_ABORT ON";
}
