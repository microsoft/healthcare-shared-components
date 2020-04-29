// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Data.SqlClient;

namespace Microsoft.Health.SqlServer.Extensions
{
    public static class SqlExceptionExtensions
    {
        /// <summary>
        /// Determins whether the exception is transient.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns><c>true</c> if the exception is transient; otherwise, <c>false</c>.</returns
        /// <remarks>
        /// Inspired by https://github.com/Azure/elastic-db-tools/blob/master/Src/ElasticScale.Client/ElasticScale.Common/TransientFaultHandling/Implementation/SqlDatabaseTransientErrorDetectionStrategy.cs.
        /// </remarks>
        public static bool IsTransient(this SqlException exception)
        {
            // Enumerate through all errors found in the exception.
            foreach (SqlError err in exception.Errors)
            {
                switch (err.Number)
                {
                    // SQL Error Code: 10928
                    // Resource ID: %d. The %s limit for the database is %d and has been reached.
                    case 10928:
                    // SQL Error Code: 10929
                    // Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d.
                    // However, the server is currently too busy to support requests greater than %d for this database.
                    case 10929:
                    // SQL Error Code: 10053
                    // A transport-level error has occurred when receiving results from the server.
                    // An established connection was aborted by the software in your host machine.
                    case 10053:
                    // SQL Error Code: 10054
                    // A transport-level error has occurred when sending the request to the server.
                    // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                    case 10054:
                    // SQL Error Code: 10060
                    // A network-related or instance-specific error occurred while establishing a connection to SQL Server.
                    // The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server
                    // is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed
                    // because the connected party did not properly respond after a period of time, or established connection failed
                    // because connected host has failed to respond.)"}
                    case 10060:
                    // SQL Error Code: 18401
                    // Login failed for user '%s'. Reason: Server is in script upgrade mode. Only administrator can connect at this time.
                    // Devnote: this can happen when SQL is going through recovery (e.g. after failover)
                    case 18401:
                    // SQL Error Code: 40197
                    // The service has encountered an error processing your request. Please try again.
                    case 40197:
                    // SQL Error Code: 40540
                    // The service has encountered an error processing your request. Please try again.
                    case 40540:
                    // SQL Error Code: 40613
                    // Database XXXX on server YYYY is not currently available. Please retry the connection later. If the problem persists, contact customer
                    // support, and provide them the session tracing ID of ZZZZZ.
                    case 40613:
                    // SQL Error Code: 40143
                    // The service has encountered an error processing your request. Please try again.
                    case 40143:
                    // SQL Error Code: 233
                    // The client was unable to establish a connection because of an error during connection initialization process before login.
                    // Possible causes include the following: the client tried to connect to an unsupported version of SQL Server; the server was too busy
                    // to accept new connections; or there was a resource limitation (insufficient memory or maximum allowed connections) on the server.
                    // (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
                    case 233:
                    // SQL Error Code: 64
                    // A connection was successfully established with the server, but then an error occurred during the login process.
                    // (provider: TCP Provider, error: 0 - The specified network name is no longer available.)
                    case 64:
                        return true;
                }
            }

            return false;
        }
    }
}
