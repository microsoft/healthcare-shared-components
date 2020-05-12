// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Microsoft.Health.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.SqlServer.Features.Schema.Messages.Get
{
    public class GetCompatibilityVersionResponse
    {
        public GetCompatibilityVersionResponse(CompatibleVersions versions)
        {
            EnsureArg.IsNotNull(versions, nameof(versions));

            CompatibleVersions = versions;
        }

        public CompatibleVersions CompatibleVersions { get; }
    }
}
