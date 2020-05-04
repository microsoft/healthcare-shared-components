// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Get;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.SqlServer.Api.Features
{
    public class CurrentVersionHandler : IRequestHandler<GetCurrentVersionRequest, GetCurrentVersionResponse>
    {
        private readonly ISchemaDataStore _schemaDataStore;

        public CurrentVersionHandler(ISchemaDataStore schemaMigrationDataStore)
        {
            EnsureArg.IsNotNull(schemaMigrationDataStore, nameof(schemaMigrationDataStore));
            _schemaDataStore = schemaMigrationDataStore;
        }

        public async Task<GetCurrentVersionResponse> Handle(GetCurrentVersionRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            List<CurrentVersionInformation> currentVersions = await _schemaDataStore.GetCurrentVersionAsync(cancellationToken);

            return new GetCurrentVersionResponse(currentVersions);
        }
    }
}
