// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Configs
{
    public class SqlServerSchemaOptions
    {
        /// <summary>
        /// Allows the automatic schema updates to apply
        /// </summary>
        public bool AutomaticUpdatesEnabled { get; set; }

        /// <summary>
        /// Allows the polling frequency for the schema updates
        /// </summary>
        public int JobPollingFrequencyInSeconds { get; set; } = 60;

        /// <summary>
        /// Allows the expired instance record to delete
        /// </summary>
        public int InstanceRecordExpirationTimeInMinutes { get; set; } = 2;
    }
}
