// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Configs;

public enum SqlServerAuthenticationType
{
    ManagedIdentity,

    /// <summary>
    /// Works for
    /// 1. Windows Integrated Authentication
    /// 2. Sql User name and Password
    /// as they both completely rely on the connection string.
    /// </summary>
    ConnectionString,

    WorkloadIdentity,
}
