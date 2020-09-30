// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Configs
{
    public class SqlServerDataStoreConfiguration
    {
        public string ConnectionString { get; set; }

        /// <summary>
        /// Allows the experimental schema initializer to attempt to bring the schema to the minimum supported version.
        /// </summary>
        public bool Initialize { get; set; }

        /// <summary>
        /// Allows the experimental schema initializer to attempt to create the database if not present.
        /// </summary>
        public bool AllowDatabaseCreation { get; set; }

        /// <summary>
        /// Connection type to connect to Sql Server and Sql DB.
        /// </summary>
        public SqlServerConnectionType ConnectionType { get; set; } = SqlServerConnectionType.Default;

        /// <summary>
        /// Configuration for transient fault retry policy.
        /// </summary>
        public SqlServerTransientFaultRetryPolicyConfiguration TransientFaultRetryPolicy { get; set; } = new SqlServerTransientFaultRetryPolicyConfiguration();

        /// <summary>
        /// Updates the schema migration options
        /// </summary>
        public SqlServerSchemaOptions SchemaOptions { get; set; } = new SqlServerSchemaOptions();
    }
}
