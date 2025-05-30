// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Storage;

/// <summary>
/// Known SQL Server error codes
/// </summary>
public static class SqlErrorCodes
{
    /// <summary>
    /// Custom error cores must be >= this
    /// </summary>
    private const int CustomErrorCodeBase = 50000;

    /// <summary>
    /// Client error
    /// </summary>
    public const int BadRequest = CustomErrorCodeBase + 400;

    /// <summary>
    /// A resource was not found
    /// </summary>
    public const int NotFound = CustomErrorCodeBase + 404;

    /// <summary>
    /// The client used an unacceptable HTTP method during the request
    /// </summary>
    public const int MethodNotAllowed = CustomErrorCodeBase + 405;

    /// <summary>
    /// The resource already exists
    /// </summary>
    public const int Conflict = CustomErrorCodeBase + 409;

    /// <summary>
    /// An optimistic concurrency precondition failed
    /// </summary>
    public const int PreconditionFailed = CustomErrorCodeBase + 412;

    /// <summary>
    /// DBNetlib error value for timeout
    /// </summary>
    public const short TimeoutExpired = -2;

    /// <summary>
    /// Cannot continue the execution because the session is in the kill state.
    /// </summary>
    public const short KilledSessionState = 596;

    /// <summary>
    /// Could not found stored procedure
    /// </summary>
    public const short CouldNotFoundStoredProc = 2812;

    /// <summary>
    /// The query processor ran out of internal resources and could not produce a query plan.
    /// </summary>
    public const short QueryProcessorNoQueryPlan = 8623;

    /// <summary>
    /// Cannot insert duplicate key row in object with unique index.
    /// </summary>
    public const short UniqueKeyConstraintViolation = 2601;

    /// <summary>
    /// Database '%.*ls' is not accessible due to Azure Key Vault critical error.
    /// </summary>
    public const int KeyVaultCriticalError = 40981;

    /// <summary>
    /// The Azure Key Vault client encountered an error with message '%s'.
    /// </summary>
    public const int KeyVaultEncounteredError = 33183;

    /// <summary>
    /// An error occurred while obtaining information for the Azure Key Vault client with message '%s'.
    /// </summary>
    public const int KeyVaultErrorObtainingInfo = 33184;

    /// <summary>
    /// Can not connect to the database in its current state.
    /// </summary>
    public const int CannotConnectToDBInCurrentState = 40925;

    /// <summary>
    /// The incoming request has too many parameters. The server supports a maximum of %d parameters.
    /// </summary>
    public const short TooManyParameters = 8003;
}
