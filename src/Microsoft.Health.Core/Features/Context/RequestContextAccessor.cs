﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Microsoft.Health.Core.Features.Context
{
    public abstract class RequestContextAccessor<T>
    {
        public abstract T RequestContext { get; set; }
    }
}
