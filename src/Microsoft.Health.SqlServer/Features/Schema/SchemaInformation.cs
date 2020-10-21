﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.SqlServer.Features.Schema
{
    public class SchemaInformation
    {
        public SchemaInformation(int minimumSupportedVersion, int maximumSupportedVersion)
        {
            MinimumSupportedVersion = minimumSupportedVersion;
            MaximumSupportedVersion = maximumSupportedVersion;
        }

        public int MinimumSupportedVersion { get; }

        public int MaximumSupportedVersion { get; }

        public int? Current { get; set; }

        /// <summary>
        /// Checks if the version is not inside the full schema script.
        /// </summary>
        /// <param name="version">The schema version.</param>
        public static bool DoesNotContainsVersionInSchema(int version)
        {
            return version > 5;
        }
    }
}