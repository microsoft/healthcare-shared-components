// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Health.SqlServer.Features.Schema.Manager.Model;

namespace Microsoft.Health.SqlServer.Features.Schema.Manager;

public interface ISchemaClient
{
    void SetUri(Uri uri);

    Task<List<CurrentVersion>> GetCurrentVersionInformationAsync(CancellationToken cancellationToken);

    Task<string> GetScriptAsync(Uri scriptUri, CancellationToken cancellationToken);

    Task<string> GetDiffScriptAsync(Uri diffScriptUri, CancellationToken cancellationToken);

    Task<CompatibleVersion> GetCompatibilityAsync(CancellationToken cancellationToken);

    Task<List<AvailableVersion>> GetAvailabilityAsync(CancellationToken cancellationToken);
}
