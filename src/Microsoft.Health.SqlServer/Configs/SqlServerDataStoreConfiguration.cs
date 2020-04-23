﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;

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
        /// WARNING: THIS RESETS ALL DATA IN THE DATABASE
        /// If set, this applies schema 1 which resets all the data in the database. This is temporary until the schema migration tool is complete.
        /// </summary>
        public bool DeleteAllDataOnStartup { get; set; }

        /// <summary>
        /// Allows the experimental schema initializer to attempt to create the database if not present.
        /// </summary>
        public bool AllowDatabaseCreation { get; set; }

        /// <summary>
        /// If set, the automatic schema updates are turned off
        /// </summary>
        public bool SchemaUpdatesDisabled { get; set; }

        /// <summary>
        /// Allows the polling frequency for the schema updates
        /// </summary>
        public TimeSpan SchemaUpdatesJobPollingFrequency { get; set; } = TimeSpan.FromMinutes(1);
    }
}
