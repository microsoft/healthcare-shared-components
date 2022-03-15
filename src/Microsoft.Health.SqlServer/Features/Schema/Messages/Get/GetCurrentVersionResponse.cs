// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema.Model;

#pragma warning disable CA1716 // Identifiers should not match keywords

namespace Microsoft.Health.SqlServer.Features.Schema.Messages.Get;

public class GetCurrentVersionResponse
{
    public GetCurrentVersionResponse(IList<CurrentVersionInformation> currentVersions)
    {
        EnsureArg.IsNotNull(currentVersions, nameof(currentVersions));

        CurrentVersions = currentVersions;
    }

    public IList<CurrentVersionInformation> CurrentVersions { get; }
}
