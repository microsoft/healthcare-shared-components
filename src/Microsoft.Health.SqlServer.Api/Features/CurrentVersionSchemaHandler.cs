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

namespace Microsoft.Health.SqlServer.Api.Features
{
    public class CurrentVersionSchemaHandler : IRequestHandler<GetCurrentVersionRequest, GetCurrentVersionResponse>
    {
        private readonly ISchemaDataStore _schemaMigrationDataStore;

        public CurrentVersionSchemaHandler(ISchemaDataStore schemaMigrationDataStore)
        {
            EnsureArg.IsNotNull(schemaMigrationDataStore, nameof(schemaMigrationDataStore));
            _schemaMigrationDataStore = schemaMigrationDataStore;
        }

        public async Task<GetCurrentVersionResponse> Handle(GetCurrentVersionRequest request, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(request, nameof(request));

            return await _schemaMigrationDataStore.GetCurrentVersionAsync(cancellationToken);
        }
    }
}
