﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.SqlServer.Api.Features;

public class CompatibilityVersionHandler : IRequestHandler<GetCompatibilityVersionRequest, GetCompatibilityVersionResponse>
{
    private readonly ISchemaDataStore _schemaDataStore;

    public CompatibilityVersionHandler(ISchemaDataStore schemaDataStore)
    {
        EnsureArg.IsNotNull(schemaDataStore, nameof(schemaDataStore));
        _schemaDataStore = schemaDataStore;
    }

    public async Task<GetCompatibilityVersionResponse> Handle(GetCompatibilityVersionRequest request, CancellationToken cancellationToken)
    {
        EnsureArg.IsNotNull(request, nameof(request));

        CompatibleVersions compatibleVersions = await _schemaDataStore.GetLatestCompatibleVersionsAsync(cancellationToken);

        return new GetCompatibilityVersionResponse(compatibleVersions);
    }
}
