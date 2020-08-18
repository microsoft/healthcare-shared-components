﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Health.Core.Features.Security
{
    public interface IClaimsExtractor
    {
        IReadOnlyCollection<KeyValuePair<string, string>> Extract();
    }
}
