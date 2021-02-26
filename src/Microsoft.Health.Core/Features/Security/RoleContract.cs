// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Core.Features.Security
{
    public class RoleContract
    {
        public string Name { get; set; }

        public IEnumerable<string> DataActions { get; set; }

        public IEnumerable<string> NotDataActions { get; set; }

        public IEnumerable<string> Scopes { get; set; }
    }
}