// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Azure;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.Encryption.Customer.Health;

public static class CustomerKeyConstants
{
    public static Func<Exception, bool> StorageAccountExceptionFilter => ex => ex is RequestFailedException rfe && rfe.ErrorCode == "KeyVaultEncryptionKeyNotFound";

    /// <summary>
    /// Filter on error codes for azure key vault https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors-31000-to-41399?view=sql-server-ver16
    /// Intentionally leaving out code 40925 when the DB is Inaccessible so that can be reported using a different HealthStatusReason
    /// </summary>
    public static Func<Exception, bool> SQLExceptionFilter => ex => ex is SqlException sqlException && (sqlException.ErrorCode == 40981 || sqlException.ErrorCode == 33183 || sqlException.ErrorCode == 33184);
}
