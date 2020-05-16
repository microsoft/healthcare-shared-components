﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SchemaManager.Model;

namespace SchemaManager
{
    public interface ISchemaClient
    {
        Task<List<CurrentVersion>> GetCurrentVersionInformation();

        Task<string> GetScript(Uri scriptUri);

        Task<string> GetDiffScript(Uri diffScriptUri);

        Task<CompatibleVersion> GetCompatibility();

        Task<List<AvailableVersion>> GetAvailability();
    }
}
