// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Features.Storage;

public static class SqlErrorExtensions
{
    public static bool IsCMKError(this SqlException sqlEx)
    {
        return sqlEx?.Number is SqlErrorCodes.KeyVaultCriticalError or SqlErrorCodes.KeyVaultEncounteredError or SqlErrorCodes.KeyVaultErrorObtainingInfo or SqlErrorCodes.CannotConnectToDBInCurrentState;
    }
}
