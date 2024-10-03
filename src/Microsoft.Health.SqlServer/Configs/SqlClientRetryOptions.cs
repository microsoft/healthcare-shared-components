// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Configs;

/// <summary>
/// Options class used to initialize the SqlClient Retry policy
/// </summary>
public class SqlClientRetryOptions
{
    public const string Retry = "Retry";

    public SqlRetryMode Mode { get; set; } = SqlRetryMode.Exponential;

    /// <summary>
    /// Default configuration are as per Microsoft recommendation stated as:
    /// It is strongly recommended that your client program has retry logic so that it could reestablish a connection after giving the transient fault time to correct itself.
    /// We recommend that you delay for 5 seconds before your first retry. Retrying after a delay shorter than 5-seconds risks overwhelming the cloud service.
    /// For each subsequent retry the delay should grow exponentially, up to a maximum of 60 seconds. More detail is found in the links below
    /// https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues?view=azuresql#implementing-retry-logic
    /// https://github.com/Huachao/azure-content/blob/master/articles/sql-database/sql-database-connect-central-recommendations.md
    ///  Default transient error codes here
    ///  https://github.com/dotnet/SqlClient/blob/main/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/Reliability/SqlConfigurableRetryFactory.cs
    /// </summary>
    public SqlRetryLogicOption Settings { get; set; } = new SqlRetryLogicOption
    {
        NumberOfTries = 5,
        DeltaTime = TimeSpan.FromSeconds(5),
        MaxTimeInterval = TimeSpan.FromSeconds(60),

        // TODO: The following setting of the TransientErrors is temporary until the transient error retry functionality in Microsoft.Data.SqlClient is complete.
        // From https://github.com/dotnet/SqlClient/blob/c6821c35c2c4038f4ab74c8da615434c81d682a4/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/Reliability/SqlConfigurableRetryFactory.cs
        // it is clear that in the future Microsoft.Data.SqlClient will allow the user to examine the exception thrown by the SQL server, in order to decide if the sql statement that caused
        // the exception should be retried. This is in addition to the list of default errors that always cause the retry. The current implementation of the Microsoft.Data.SqlClient does not allow
        // us to add more errors to the list of the existing errors, we can only replace the list of default errors with our own list.
        // So, as a temporary solution we copy in here the list of default retriable errors from Microsoft.Data.SqlClient and add our own retriable errors, and then supply the entire list to the
        // SqlRetryLogicOption. In the meantime, until Microsoft.Data.SqlClient retry functionality is finished we periodicaly check Microsoft.Data.SqlClient to see if the default list of errors
        // has changed in order to update the list in here. And once Microsoft.Data.SqlClient is finished we remove this temporary solution and modify our code to properly handle additional
        // retriable transient errors.

        // Default errors copied from src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/Reliability/SqlConfigurableRetryFactory.cs.
        TransientErrors = new HashSet<int>
                {
                    // Default .NET errors:
                    1204,   // The instance of the SQL Server Database Engine cannot obtain a LOCK resource at this time. Rerun your statement when there are fewer active users. Ask the database administrator to check the lock and memory configuration for this instance, or to check for long-running transactions.
                    1205,   // Transaction (Process ID) was deadlocked on resources with another process and has been chosen as the deadlock victim. Rerun the transaction.
                    1222,   // Lock request time out period exceeded.
                    49918,  // Cannot process request. Not enough resources to process request.
                    49919,  // Cannot process create or update request. Too many create or update operations in progress for subscription "%ld".
                    49920,  // Cannot process request. Too many operations in progress for subscription "%ld".
                    4060,   // Cannot open database "%.*ls" requested by the login. The login failed.
                    4221,   // Login to read-secondary failed due to long wait on 'HADR_DATABASE_WAIT_FOR_TRANSITION_TO_VERSIONING'. The replica is not available for login because row versions are missing for transactions that were in-flight when the replica was recycled. The issue can be resolved by rolling back or committing the active transactions on the primary replica. Occurrences of this condition can be minimized by avoiding long write transactions on the primary.
                    40143,  // The service has encountered an error processing your request. Please try again.
                    40613,  // Database '%.*ls' on server '%.*ls' is not currently available. Please retry the connection later. If the problem persists, contact customer support, and provide them the session tracing ID of '%.*ls'.
                    40501,  // The service is currently busy. Retry the request after 10 seconds. Incident ID: %ls. Code: %d.
                    40540,  // The service has encountered an error processing your request. Please try again.
                    40197,  // The service has encountered an error processing your request. Please try again. Error code %d.
                    42108,  // Can not connect to the SQL pool since it is paused. Please resume the SQL pool and try again.
                    42109,  // The SQL pool is warming up. Please try again.
                    10929,  // Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d. However, the server is currently too busy to support requests greater than %d for this database. For more information, see http://go.microsoft.com/fwlink/?LinkId=267637. Otherwise, please try again later.
                    10928,  // Resource ID: %d. The %s limit for the database is %d and has been reached. For more information, see http://go.microsoft.com/fwlink/?LinkId=267637.
                    10060,  // An error has occurred while establishing a connection to the server. When connecting to SQL Server, this failure may be caused by the fact that under the default settings SQL Server does not allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond.) (Microsoft SQL Server, Error: 10060)
                    997,    // A connection was successfully established with the server, but then an error occurred during the login process. (provider: Named Pipes Provider, error: 0 - Overlapped I/O operation is in progress)
                    233,    // A connection was successfully established with the server, but then an error occurred during the login process. (provider: Shared Memory Provider, error: 0 - No process is on the other end of the pipe.) (Microsoft SQL Server, Error: 233)

                    // Additional .NET errors:
                    35,     // A connection was successfully established with the server, but then an error occurred during the login process. (provider: TCP Provider, error: 35 - An internal exception was caught)
                    18456,  // Login failed for user '<token-identified principal>'.
                    258, // System.ComponentModel.Win32Exception (258): Unknown error 258

                    // Additional Fhir Server errors:
                    8623    // The query processor ran out of internal resources and could not produce a query plan.
                },
    };
}
