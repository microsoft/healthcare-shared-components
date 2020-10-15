// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SchemaManager.Model;

namespace SchemaManager
{
    public interface ISchemaClient
    {
        Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken);

        Task<string> GetScriptAsync(Uri scriptUri, CancellationToken cancellationToken);

        Task<string> GetDiffScriptAsync(Uri diffScriptUri, CancellationToken cancellationToken);

        Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken);

        Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken);
    }
}
